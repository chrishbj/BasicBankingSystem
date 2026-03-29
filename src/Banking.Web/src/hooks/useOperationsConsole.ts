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

export function useOperationsConsole() {
  const [health, setHealth] = useState<Record<string, string>>({})
  const [message, setMessage] = useState('Ready.')
  const [depositStatusText, setDepositStatusText] = useState('No deposit submitted yet.')
  const [reviewStatusText, setReviewStatusText] = useState('Load the queue to inspect pending review items.')
  const [customer, setCustomer] = useState<CustomerResponse | null>(null)
  const [account, setAccount] = useState<AccountResponse | null>(null)
  const [deposit, setDeposit] = useState<DepositResponse | null>(null)
  const [depositSearchResult, setDepositSearchResult] = useState<DepositResponse[]>([])
  const [pendingReviewItems, setPendingReviewItems] = useState<PendingReviewDepositSummaryResponse[]>([])
  const [sortBy, setSortBy] = useState<PendingReviewSortBy>('ReviewRequiredAt')
  const [descending, setDescending] = useState(false)
  const [reviewSearch, setReviewSearch] = useState<ReviewSearchState>({
    correlationId: '',
    failureCode: '',
    status: 'PendingReview',
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

  async function runAction(label: string, action: () => Promise<void>) {
    try {
      setMessage(`${label} in progress...`)
      await action()
      setMessage(`${label} completed.`)
    } catch (error) {
      setMessage(`${label} failed: ${error instanceof Error ? error.message : String(error)}`)
    }
  }

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
        fullName: customerForm.fullName,
        identityType: 'NationalId',
        identityNumber: customerForm.identityNumber,
        mobile: customerForm.mobile,
        email: customerForm.email,
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
      setDeposit(null)
      setDepositStatusText('Customer created. Open an account to continue.')
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
      setDepositStatusText('Account opened. You can submit a deposit now.')
    })
  }

  async function handleRefreshAccount() {
    if (!account) {
      setMessage('Open an account first.')
      return
    }

    await runAction('Refresh account', async () => {
      setAccount(await getAccount(account.accountId))
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
            amount: Number(depositForm.amount),
            currency: account.currency,
            channel: 1,
            referenceNumber: depositForm.referenceNumber,
            note: depositForm.note,
          },
          `web-idem-${Date.now()}`,
          `web-corr-${Date.now()}`,
        ),
      )
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
    depositStatusText,
    reviewStatusText,
    customer,
    account,
    deposit,
    depositSearchResult,
    pendingReviewItems,
    sortBy,
    descending,
    reviewSearch,
    customerForm,
    depositForm,
    setSortBy,
    setDescending,
    setReviewSearch,
    setCustomerForm,
    setDepositForm,
    loadHealth,
    handleCreateCustomer,
    handleActivateCustomer,
    handleOpenAccount,
    handleRefreshAccount,
    handleSubmitDeposit,
    handleRefreshDeposit,
    handleSearchDeposits,
    handleLoadPendingReview,
    handleRetryPendingReview,
    handleResolvePendingReview,
  }
}
