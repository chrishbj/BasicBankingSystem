import { useEffect, useState } from 'react'
import {
  activateCustomer,
  createCustomer,
  getAccount,
  getAccountsByCustomer,
  getCustomers,
  getDeposit,
  getHealth,
  getPendingReview,
  openAccount,
  resolvePendingReview,
  retryPendingReview,
  searchDeposits,
  submitDeposit,
} from '../api'
import type {
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  DepositResponse,
  DepositSummaryResponse,
  PendingReviewDepositSummaryResponse,
  PendingReviewSortBy,
} from '../types'

type CustomerFormState = {
  fullName: string
  identityNumber: string
  mobile: string
  email: string
}

type DepositFormState = {
  amount: string
  referenceNumber: string
  note: string
}

type ReviewSearchState = {
  correlationId: string
  failureCode: string
  status: string
}

type AccountQueryState = {
  accountId: string
}

type AccountHistoryFilterState = {
  status: string
  requestedFrom: string
  requestedTo: string
}

export function useOperationsConsole() {
  const [health, setHealth] = useState<Record<string, string>>({})
  const [message, setMessage] = useState('Ready.')
  const [toast, setToast] = useState('')
  const [busyAction, setBusyAction] = useState('')
  const [customerStatusText, setCustomerStatusText] = useState('Create or browse customers, then select one to inspect accounts and activity.')
  const [depositStatusText, setDepositStatusText] = useState('No deposit submitted yet.')
  const [accountHistoryStatusText, setAccountHistoryStatusText] = useState('Look up an account to inspect balances and history.')
  const [reviewStatusText, setReviewStatusText] = useState('Load the queue to inspect pending review items.')
  const [customer, setCustomer] = useState<CustomerResponse | null>(null)
  const [customers, setCustomers] = useState<CustomerResponse[]>([])
  const [account, setAccount] = useState<AccountResponse | null>(null)
  const [accountList, setAccountList] = useState<AccountSummaryResponse[]>([])
  const [deposit, setDeposit] = useState<DepositResponse | null>(null)
  const [accountHistory, setAccountHistory] = useState<DepositSummaryResponse[]>([])
  const [selectedAccountHistoryItem, setSelectedAccountHistoryItem] = useState<DepositSummaryResponse | null>(null)
  const [depositSearchResult, setDepositSearchResult] = useState<DepositResponse[]>([])
  const [pendingReviewItems, setPendingReviewItems] = useState<PendingReviewDepositSummaryResponse[]>([])
  const [sortBy, setSortBy] = useState<PendingReviewSortBy>('ReviewRequiredAt')
  const [descending, setDescending] = useState(false)
  const [reviewSearch, setReviewSearch] = useState<ReviewSearchState>({
    correlationId: '',
    failureCode: '',
    status: 'PendingReview',
  })
  const [accountQuery, setAccountQuery] = useState<AccountQueryState>({
    accountId: '',
  })
  const [accountHistoryFilters, setAccountHistoryFilters] = useState<AccountHistoryFilterState>({
    status: '',
    requestedFrom: '',
    requestedTo: '',
  })
  const [customerForm, setCustomerForm] = useState<CustomerFormState>({
    fullName: 'Frontend Demo Customer',
    identityNumber: `WEB-${Date.now()}`,
    mobile: '13800000000',
    email: 'frontend.demo@example.com',
  })
  const [depositForm, setDepositForm] = useState<DepositFormState>({
    amount: '500',
    referenceNumber: `WEB-REF-${Date.now()}`,
    note: 'Created from the React operations console.',
  })

  useEffect(() => {
    if (!toast) {
      return
    }

    const handle = window.setTimeout(() => setToast(''), 3000)
    return () => window.clearTimeout(handle)
  }, [toast])

  useEffect(() => {
    void loadHealth()
    void loadCustomers()
  }, [])

  useEffect(() => {
    if (!deposit || ![1, 2].includes(deposit.status)) {
      return
    }

    setDepositStatusText('Deposit is still processing. Auto-refresh is active.')

    const handle = window.setInterval(async () => {
      try {
        const refreshed = await getDeposit(deposit.transactionId)
        setDeposit(refreshed)

        if (![1, 2].includes(refreshed.status)) {
          setDepositStatusText(`Deposit finished with status ${refreshed.status}.`)
          window.clearInterval(handle)
        }
      } catch (error) {
        setDepositStatusText(`Deposit auto-refresh failed: ${error instanceof Error ? error.message : String(error)}`)
        window.clearInterval(handle)
      }
    }, 2000)

    return () => window.clearInterval(handle)
  }, [deposit])

  useEffect(() => {
    if (!account) {
      return
    }

    setAccountQuery({ accountId: account.accountId })
  }, [account])

  async function runAction(label: string, action: () => Promise<void>) {
    try {
      setBusyAction(label)
      setMessage(`${label} in progress...`)
      await action()
      setMessage(`${label} completed.`)
      setToast(`${label} completed.`)
    } catch (error) {
      const messageText = `${label} failed: ${error instanceof Error ? error.message : String(error)}`
      setMessage(messageText)
      setToast(messageText)
    } finally {
      setBusyAction('')
    }
  }

  const trimmedCustomerForm = {
    fullName: customerForm.fullName.trim(),
    identityNumber: customerForm.identityNumber.trim(),
    mobile: customerForm.mobile.trim(),
    email: customerForm.email.trim(),
  }

  const depositAmount = Number(depositForm.amount)
  const customerFormErrors = {
    fullName: trimmedCustomerForm.fullName ? '' : 'Full name is required.',
    identityNumber: trimmedCustomerForm.identityNumber ? '' : 'Identity number is required.',
    mobile: trimmedCustomerForm.mobile ? '' : 'Mobile is required.',
    email: trimmedCustomerForm.email.includes('@') ? '' : 'Email must be valid.',
  }
  const depositFormErrors = {
    amount: Number.isFinite(depositAmount) && depositAmount > 0 ? '' : 'Amount must be greater than zero.',
    referenceNumber: depositForm.referenceNumber.trim() ? '' : 'Reference number is required.',
  }
  const canCreateCustomer = Object.values(customerFormErrors).every((item) => !item) && !busyAction
  const canSubmitDeposit = !!customer && !!account && Object.values(depositFormErrors).every((item) => !item) && !busyAction

  async function loadHealth() {
    const services = [
      ['customer', '/customer-api'],
      ['account', '/account-api'],
      ['deposit', '/deposit-api'],
      ['audit', '/audit-api'],
    ] as const

    const entries = await Promise.all(
      services.map(async ([name, basePath]) => {
        try {
          const value = await getHealth(basePath)
          return [name, value] as const
        } catch (error) {
          return [name, error instanceof Error ? error.message : String(error)] as const
        }
      }),
    )

    setHealth(Object.fromEntries(entries))
  }

  async function handleCreateCustomer() {
    await runAction('Create customer', async () => {
      const created = await createCustomer({
        fullName: trimmedCustomerForm.fullName,
        identityType: 'NationalId',
        identityNumber: trimmedCustomerForm.identityNumber,
        mobile: trimmedCustomerForm.mobile,
        email: trimmedCustomerForm.email,
        address: {
          country: 'CN',
          province: 'Shanghai',
          city: 'Shanghai',
          line1: 'No. 8 Riverfront Avenue',
          postalCode: '200000',
        },
        riskLevel: 'Low',
      })

      setCustomer(created)
      await loadCustomers(created.customerId)
      setAccount(null)
      setAccountList([])
      setAccountHistory([])
      setSelectedAccountHistoryItem(null)
      setAccountQuery({ accountId: '' })
      setDeposit(null)
      setCustomerStatusText(`Customer ${created.fullName} created and selected.`)
      setDepositStatusText('Customer created. Open an account to continue.')
      setAccountHistoryStatusText('Customer created. Open or look up an account to inspect history.')
    })
  }

  async function handleActivateCustomer() {
    if (!customer) {
      setMessage('Create a customer first.')
      return
    }

    await runAction('Activate customer', async () => {
      const updated = await activateCustomer(customer.customerId, 'React console activation')
      setCustomer(updated)
      await loadCustomers(updated.customerId)
      setCustomerStatusText(`Customer ${updated.fullName} activated.`)
    })
  }

  async function handleOpenAccount() {
    if (!customer) {
      setMessage('Create and activate a customer first.')
      return
    }

    await runAction('Open account', async () => {
      setAccount(
        await openAccount({
          customerId: customer.customerId,
          accountType: 'Checking',
          currency: 'CNY',
        }),
      )
      await loadCustomerAccountsCore(customer.customerId)
      setAccountHistory([])
      setSelectedAccountHistoryItem(null)
      setAccountHistoryStatusText('Account opened. Load history to inspect transactions on this account.')
      setDepositStatusText('Account opened. You can submit a deposit now.')
    })
  }

  async function handleRefreshAccount() {
    if (!account) {
      setMessage('Open an account first.')
      return
    }

    await runAction('Refresh account', async () => {
      const refreshed = await getAccount(account.accountId)
      setAccount(refreshed)
      setAccountHistoryStatusText(`Refreshed account ${refreshed.accountId}.`)
    })
  }

  async function handleLookupAccount() {
    const accountId = accountQuery.accountId.trim()
    if (!accountId) {
      setMessage('Enter an account ID first.')
      return
    }

    await runAction('Lookup account', async () => {
      const fetched = await getAccount(accountId)
      setAccount(fetched)
      await loadCustomerAccountsCore(fetched.customerId)
      setSelectedAccountHistoryItem(null)
      setAccountHistoryStatusText(`Loaded account ${fetched.accountId}.`)
    })
  }

  async function loadCustomerAccountsCore(customerId: string) {
    const response = await getAccountsByCustomer(customerId)
    setAccountList(response.items)
    return response.items
  }

  async function loadCustomers(selectedCustomerId?: string) {
    const response = await getCustomers()
    setCustomers(response.items)

    if (selectedCustomerId) {
      const selected = response.items.find((item) => item.customerId === selectedCustomerId)
      if (selected) {
        setCustomer(selected)
      }
    }
  }

  async function handleLoadCustomers() {
    await runAction('Load customers', async () => {
      await loadCustomers(customer?.customerId)
      setCustomerStatusText('Loaded current customer directory.')
    })
  }

  async function handleSelectCustomer(nextCustomer: CustomerResponse) {
    await runAction('Load customer workspace', async () => {
      setCustomer(nextCustomer)
      setDeposit(null)
      setSelectedAccountHistoryItem(null)

      const items = await loadCustomerAccountsCore(nextCustomer.customerId)

      if (items.length > 0) {
        const primaryAccountId = items[0].accountId
        const fetched = await getAccount(primaryAccountId)
        setAccount(fetched)
        setAccountQuery({ accountId: primaryAccountId })
        await loadAccountHistoryCore(primaryAccountId, nextCustomer.customerId)
        setAccountHistoryStatusText(`Loaded ${items.length} accounts and recent activity for ${nextCustomer.fullName}.`)
      }
      else
      {
        setAccount(null)
        setAccountQuery({ accountId: '' })
        setAccountHistory([])
        setSelectedAccountHistoryItem(null)
        setAccountHistoryStatusText(`Customer ${nextCustomer.fullName} does not have any accounts yet.`)
      }

      setCustomerStatusText(`Selected ${nextCustomer.fullName} and loaded related data.`)
    })
  }

  async function handleLoadCustomerAccounts() {
    const targetCustomerId = customer?.customerId ?? account?.customerId
    if (!targetCustomerId) {
      setMessage('Create or load a customer first.')
      return
    }

    await runAction('Load customer accounts', async () => {
      await loadCustomerAccountsCore(targetCustomerId)
      setAccountHistoryStatusText(`Loaded accounts for customer ${targetCustomerId}.`)
    })
  }

  async function handleSelectAccount(accountId: string) {
    await runAction('Switch account', async () => {
      const fetched = await getAccount(accountId)
      setAccount(fetched)
      setAccountQuery({ accountId })
      await loadAccountHistoryCore(accountId, fetched.customerId)
      setAccountHistoryStatusText(`Loaded account ${accountId} and its history.`)
    })
  }

  async function loadAccountHistoryCore(accountId: string, customerId?: string) {
    const params = new URLSearchParams({
      pageNumber: '1',
      pageSize: '20',
      accountId,
    })

    if (customerId) {
      params.set('customerId', customerId)
    }

    if (accountHistoryFilters.status) {
      params.set('status', accountHistoryFilters.status)
    }

    if (accountHistoryFilters.requestedFrom) {
      params.set('requestedFrom', new Date(accountHistoryFilters.requestedFrom).toISOString())
    }

    if (accountHistoryFilters.requestedTo) {
      params.set('requestedTo', new Date(accountHistoryFilters.requestedTo).toISOString())
    }

    const response = await searchDeposits(params)
    setAccountHistory(response.items)
    setSelectedAccountHistoryItem(response.items[0] ?? null)
    setAccountHistoryStatusText(`Loaded ${response.items.length} deposits for account ${accountId}.`)
  }

  async function handleLoadAccountHistory() {
    const accountId = accountQuery.accountId.trim() || account?.accountId
    if (!accountId) {
      setMessage('Provide an account ID first.')
      return
    }

    await runAction('Load account history', async () => {
      const customerId = account?.customerId ?? customer?.customerId
      await loadAccountHistoryCore(accountId, customerId)
    })
  }

  async function handleSubmitDeposit() {
    if (!customer || !account) {
      setMessage('Create a customer and account first.')
      return
    }

    await runAction('Submit deposit', async () => {
      setDeposit(
        await submitDeposit(
          {
            customerId: customer.customerId,
            accountId: account.accountId,
            amount: depositAmount,
            currency: account.currency,
            channel: 1,
            referenceNumber: depositForm.referenceNumber.trim(),
            note: depositForm.note.trim(),
          },
          `web-idem-${Date.now()}`,
          `web-corr-${Date.now()}`,
        ),
      )
      await loadAccountHistoryCore(account.accountId, customer.customerId)
      setDepositStatusText('Deposit submitted. Waiting for asynchronous processing.')
    })
  }

  async function handleRefreshDeposit() {
    if (!deposit) {
      setMessage('Submit a deposit first.')
      return
    }

    await runAction('Refresh deposit', async () => {
      const refreshed = await getDeposit(deposit.transactionId)
      setDeposit(refreshed)
      setDepositStatusText(`Deposit refreshed. Current status is ${refreshed.status}.`)
    })
  }

  async function handleSearchDeposits() {
    await runAction('Search deposits', async () => {
      const params = new URLSearchParams({
        pageNumber: '1',
        pageSize: '20',
      })

      if (reviewSearch.status) {
        params.set('status', reviewSearch.status)
      }

      if (reviewSearch.correlationId) {
        params.set('correlationId', reviewSearch.correlationId)
      } else if (deposit?.correlationId) {
        params.set('correlationId', deposit.correlationId)
      }

      if (reviewSearch.failureCode) {
        params.set('failureCode', reviewSearch.failureCode)
      }

      const response = await searchDeposits(params)
      const details = await Promise.all(response.items.map((item) => getDeposit(item.transactionId)))
      setDepositSearchResult(details)
      setReviewStatusText(`Loaded ${details.length} matching deposits.`)
    })
  }

  async function handleLoadPendingReview() {
    await runAction('Load pending review queue', async () => {
      const response = await getPendingReview(sortBy, descending)
      setPendingReviewItems(response.items)
      setReviewStatusText(`Loaded ${response.items.length} pending review items.`)
    })
  }

  async function handleRetryPendingReview(transactionId: string) {
    await runAction('Retry compensation', async () => {
      setDeposit(
        await retryPendingReview(transactionId, 'frontend-ops', 'Retry requested from the React console.'),
      )
      setReviewStatusText(`Retry requested for ${transactionId}.`)
      await handleLoadPendingReview()
    })
  }

  async function handleResolvePendingReview(transactionId: string, resolution: 3 | 4) {
    await runAction('Resolve pending review', async () => {
      setDeposit(
        await resolvePendingReview(
          transactionId,
          'frontend-ops',
          resolution === 3
            ? 'Marked as externally reversed from the React console.'
            : 'Marked as externally failed from the React console.',
          resolution,
        ),
      )
      setReviewStatusText(`Review item ${transactionId} resolved.`)
      await handleLoadPendingReview()
    })
  }

  return {
    health,
    message,
    toast,
    busyAction,
    customerStatusText,
    depositStatusText,
    accountHistoryStatusText,
    reviewStatusText,
    customer,
    customers,
    account,
    accountList,
    deposit,
    accountHistory,
    selectedAccountHistoryItem,
    depositSearchResult,
    pendingReviewItems,
    sortBy,
    descending,
    reviewSearch,
    accountQuery,
    accountHistoryFilters,
    customerForm,
    depositForm,
    customerFormErrors,
    depositFormErrors,
    canCreateCustomer,
    canSubmitDeposit,
    setSortBy,
    setDescending,
    setReviewSearch,
    setAccountQuery,
    setAccountHistoryFilters,
    setCustomerForm,
    setDepositForm,
    loadHealth,
    handleLoadCustomers,
    handleSelectCustomer,
    handleCreateCustomer,
    handleActivateCustomer,
    handleOpenAccount,
    handleRefreshAccount,
    handleLookupAccount,
    handleLoadCustomerAccounts,
    handleSelectAccount,
    handleLoadAccountHistory,
    handleSubmitDeposit,
    handleRefreshDeposit,
    handleSearchDeposits,
    handleLoadPendingReview,
    handleRetryPendingReview,
    handleResolvePendingReview,
    setSelectedAccountHistoryItem,
  }
}
