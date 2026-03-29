import { useEffect, useState } from 'react'
import {
  activateCustomer,
  createCustomer,
  getAccount,
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

export function useOperationsConsole() {
  const [health, setHealth] = useState<Record<string, string>>({})
  const [message, setMessage] = useState('Ready.')
  const [toast, setToast] = useState('')
  const [busyAction, setBusyAction] = useState('')
  const [depositStatusText, setDepositStatusText] = useState('No deposit submitted yet.')
  const [accountHistoryStatusText, setAccountHistoryStatusText] = useState('Look up an account to inspect balances and history.')
  const [reviewStatusText, setReviewStatusText] = useState('Load the queue to inspect pending review items.')
  const [customer, setCustomer] = useState<CustomerResponse | null>(null)
  const [account, setAccount] = useState<AccountResponse | null>(null)
  const [deposit, setDeposit] = useState<DepositResponse | null>(null)
  const [accountHistory, setAccountHistory] = useState<DepositSummaryResponse[]>([])
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
      setAccount(null)
      setAccountHistory([])
      setAccountQuery({ accountId: '' })
      setDeposit(null)
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
      setCustomer(await activateCustomer(customer.customerId, 'React console activation'))
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
      setAccountHistory([])
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
      setAccountHistoryStatusText(`Loaded account ${fetched.accountId}.`)
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

    const response = await searchDeposits(params)
    setAccountHistory(response.items)
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
    depositStatusText,
    accountHistoryStatusText,
    reviewStatusText,
    customer,
    account,
    deposit,
    accountHistory,
    depositSearchResult,
    pendingReviewItems,
    sortBy,
    descending,
    reviewSearch,
    accountQuery,
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
    setCustomerForm,
    setDepositForm,
    loadHealth,
    handleCreateCustomer,
    handleActivateCustomer,
    handleOpenAccount,
    handleRefreshAccount,
    handleLookupAccount,
    handleLoadAccountHistory,
    handleSubmitDeposit,
    handleRefreshDeposit,
    handleSearchDeposits,
    handleLoadPendingReview,
    handleRetryPendingReview,
    handleResolvePendingReview,
  }
}
