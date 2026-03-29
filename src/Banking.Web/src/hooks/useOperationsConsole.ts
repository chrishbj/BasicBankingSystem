import { useEffect, useState } from 'react'
import {
  activateCustomer,
  createCustomer,
  createPendingReviewDemo,
  getAccount,
  getAccountByNumber,
  getAccountActivities,
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
  submitWithdrawal,
} from '../api'
import type {
  AccountActivityResponse,
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  DepositResponse,
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
  accountNumber: string
}

type AccountHistoryFilterState = {
  activityType: string
  from: string
  to: string
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
  const [accountHistory, setAccountHistory] = useState<AccountActivityResponse[]>([])
  const [customerActivitySnapshot, setCustomerActivitySnapshot] = useState<AccountActivityResponse[]>([])
  const [selectedAccountHistoryItem, setSelectedAccountHistoryItem] = useState<AccountActivityResponse | null>(null)
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
    accountNumber: '',
  })
  const [accountHistoryFilters, setAccountHistoryFilters] = useState<AccountHistoryFilterState>({
    activityType: '',
    from: '',
    to: '',
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
    void loadPendingReviewSnapshot()
  }, [])

  useEffect(() => {
    if (!deposit || ![1, 2].includes(deposit.status)) {
      return
    }

    setDepositStatusText('Deposit is still processing. Auto-refresh is active.')

    // The UI polls only while the deposit is in transient states so operators get workflow
    // progress without manually refreshing the transaction.
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

    setAccountQuery({ accountNumber: account.accountNumber })
  }, [account])

  async function runAction(label: string, action: () => Promise<void>) {
    try {
      // This hook acts like a lightweight application service for the operator workspace:
      // one place owns async actions, status text, and optimistic UI coordination.
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
      setCustomerActivitySnapshot([])
      setSelectedAccountHistoryItem(null)
      setAccountQuery({ accountNumber: '' })
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
          currency: 'USD',
        }),
      )
      await loadCustomerAccountsCore(customer.customerId)
      setAccountHistory([])
      setSelectedAccountHistoryItem(null)
      setAccountHistoryStatusText('Account opened. Load activity history to inspect transactions on this account.')
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
      setAccountHistoryStatusText(`Refreshed account ${refreshed.accountNumber}.`)
    })
  }

  async function handleLookupAccount() {
    const accountNumber = accountQuery.accountNumber.trim()
    if (!accountNumber) {
      setMessage('Enter an account number first.')
      return
    }

    await runAction('Lookup account', async () => {
      const fetched = await getAccountByNumber(accountNumber)
      setAccount(fetched)
      await loadCustomerAccountsCore(fetched.customerId)
      setSelectedAccountHistoryItem(null)
      setAccountHistoryStatusText(`Loaded account ${fetched.accountNumber}.`)
    })
  }

  async function loadCustomerAccountsCore(customerId: string) {
    const response = await getAccountsByCustomer(customerId)
    setAccountList(response.items)
    await loadCustomerActivitySnapshot(response.items)
    return response.items
  }

  async function loadCustomerActivitySnapshot(accounts: AccountSummaryResponse[]) {
    if (accounts.length === 0) {
      setCustomerActivitySnapshot([])
      return
    }

    const snapshots = await Promise.all(
      accounts.slice(0, 5).map(async (item) => {
        const params = new URLSearchParams({
          pageNumber: '1',
          pageSize: '5',
        })

        const response = await getAccountActivities(item.accountId, params)
        return response.items
      }),
    )

    const merged = snapshots
      .flat()
      .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
      .slice(0, 8)

    setCustomerActivitySnapshot(merged)
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

  async function loadPendingReviewSnapshot() {
    try {
      const response = await getPendingReview(sortBy, descending)
      setPendingReviewItems(response.items)
    } catch {
      setPendingReviewItems([])
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

      // Selecting a customer intentionally fans out into related account and activity loading,
      // because the operator workflow is context-driven rather than page-driven.
      const items = await loadCustomerAccountsCore(nextCustomer.customerId)

      if (items.length > 0) {
        const primaryAccountId = items[0].accountId
        const fetched = await getAccount(primaryAccountId)
        setAccount(fetched)
        setAccountQuery({ accountNumber: fetched.accountNumber })
        await loadAccountHistoryCore(primaryAccountId)
        setAccountHistoryStatusText(`Loaded ${items.length} accounts and recent activity for ${nextCustomer.fullName}.`)
      }
      else
      {
        setAccount(null)
        setAccountQuery({ accountNumber: '' })
        setAccountHistory([])
        setCustomerActivitySnapshot([])
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
      setAccountHistoryStatusText(`Loaded accounts for customer ${customer?.fullName ?? targetCustomerId}.`)
    })
  }

  async function handleSelectAccount(accountId: string) {
    await runAction('Switch account', async () => {
      const fetched = await getAccount(accountId)
      setAccount(fetched)
      setAccountQuery({ accountNumber: fetched.accountNumber })
      await loadAccountHistoryCore(accountId)
      setAccountHistoryStatusText(`Loaded account ${fetched.accountNumber} and its history.`)
    })
  }

  async function loadAccountHistoryCore(accountId: string) {
    const params = new URLSearchParams({
      pageNumber: '1',
      pageSize: '20',
    })

    if (accountHistoryFilters.activityType) {
      params.set('activityType', accountHistoryFilters.activityType)
    }

    if (accountHistoryFilters.from) {
      params.set('from', new Date(accountHistoryFilters.from).toISOString())
    }

    if (accountHistoryFilters.to) {
      params.set('to', new Date(accountHistoryFilters.to).toISOString())
    }

    const response = await getAccountActivities(accountId, params)
    setAccountHistory(response.items)
    setSelectedAccountHistoryItem(response.items[0] ?? null)
    setAccountHistoryStatusText(`Loaded ${response.items.length} activities for account ${account?.accountNumber ?? accountId}.`)

    if (accountList.length > 0) {
      await loadCustomerActivitySnapshot(accountList)
    }
  }

  async function handleLoadAccountHistory() {
    const accountId = account?.accountId
    if (!accountId) {
      setMessage('Select or look up an account first.')
      return
    }

    await runAction('Load account history', async () => {
      await loadAccountHistoryCore(accountId)
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
      await loadAccountHistoryCore(account.accountId)
      setDepositStatusText('Deposit submitted. Waiting for asynchronous processing.')
    })
  }

  async function handleSubmitWithdrawal() {
    if (!account) {
      setMessage('Select an account first.')
      return
    }

    await runAction('Submit withdrawal', async () => {
      const updated = await submitWithdrawal(account.accountId, {
        amount: depositAmount,
        currency: account.currency,
        referenceNumber: depositForm.referenceNumber.trim(),
        correlationId: `web-wd-corr-${Date.now()}`,
        note: depositForm.note.trim(),
      })

      setAccount(updated)
      await loadAccountHistoryCore(account.accountId)
      setDepositStatusText('Withdrawal submitted and account balance updated.')
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

  async function loadPendingReviewCore() {
    const response = await getPendingReview(sortBy, descending)
    setPendingReviewItems(response.items)
    setReviewStatusText(`Loaded ${response.items.length} pending review items.`)
  }

  async function handleLoadPendingReview() {
    await runAction('Load pending review queue', async () => {
      await loadPendingReviewCore()
    })
  }

  async function handleCreatePendingReviewDemo() {
    if (!customer || !account) {
      setMessage('Select a customer and one of its accounts first.')
      return
    }

    await runAction('Create pending review demo item', async () => {
      const demoDeposit = await createPendingReviewDemo({
        customerId: customer.customerId,
        accountId: account.accountId,
        amount: Math.max(10, depositAmount || 25),
        note: `Demo pending-review item created for ${customer.fullName}.`,
      })

      setDeposit(demoDeposit)
      await loadAccountHistoryCore(account.accountId)
      await loadPendingReviewCore()
      setDepositStatusText('Demo pending-review item created for the selected customer and account.')
    })
  }

  async function handleRetryPendingReview(transactionId: string) {
    await runAction('Retry compensation', async () => {
      setDeposit(
        await retryPendingReview(transactionId, 'frontend-ops', 'Retry requested from the React console.'),
      )
      await loadPendingReviewCore()
      setReviewStatusText(`Retry requested for ${transactionId}.`)
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
      await loadPendingReviewCore()
      setReviewStatusText(`Review item ${transactionId} resolved.`)
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
    customerActivitySnapshot,
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
    handleSubmitWithdrawal,
    handleRefreshDeposit,
    handleSearchDeposits,
    handleLoadPendingReview,
    handleCreatePendingReviewDemo,
    handleRetryPendingReview,
    handleResolvePendingReview,
    setSelectedAccountHistoryItem,
  }
}
