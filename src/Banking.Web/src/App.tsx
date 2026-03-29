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
} from './api'
import './App.css'
import { AccountPanel } from './components/AccountPanel'
import { CustomerPanel } from './components/CustomerPanel'
import { DepositPanel } from './components/DepositPanel'
import { EnvironmentPanel } from './components/EnvironmentPanel'
import { PendingReviewPanel } from './components/PendingReviewPanel'
import type {
  AccountResponse,
  CustomerResponse,
  DepositResponse,
  PendingReviewDepositSummaryResponse,
  PendingReviewSortBy,
} from './types'

function App() {
  const [health, setHealth] = useState<Record<string, string>>({})
  const [message, setMessage] = useState('Ready.')
  const [customer, setCustomer] = useState<CustomerResponse | null>(null)
  const [account, setAccount] = useState<AccountResponse | null>(null)
  const [deposit, setDeposit] = useState<DepositResponse | null>(null)
  const [depositSearchResult, setDepositSearchResult] = useState<DepositResponse[]>([])
  const [pendingReviewItems, setPendingReviewItems] = useState<PendingReviewDepositSummaryResponse[]>([])
  const [sortBy, setSortBy] = useState<PendingReviewSortBy>('ReviewRequiredAt')
  const [descending, setDescending] = useState(false)

  const [customerForm, setCustomerForm] = useState({
    fullName: 'Frontend Demo Customer',
    identityNumber: `WEB-${Date.now()}`,
    mobile: '13800000000',
    email: 'frontend.demo@example.com',
  })

  const [depositForm, setDepositForm] = useState({
    amount: '500',
    referenceNumber: `WEB-REF-${Date.now()}`,
    note: 'Created from the React operations console.',
  })

  useEffect(() => {
    void loadHealth()
  }, [])

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
    })
  }

  async function handleRefreshDeposit() {
    if (!deposit) {
      setMessage('Submit a deposit first.')
      return
    }

    await runAction('Refresh deposit', async () => {
      setDeposit(await getDeposit(deposit.transactionId))
    })
  }

  async function handleSearchDeposits() {
    await runAction('Search deposits', async () => {
      const params = new URLSearchParams({
        status: 'PendingReview',
        pageNumber: '1',
        pageSize: '20',
      })

      if (deposit?.correlationId) {
        params.set('correlationId', deposit.correlationId)
      }

      const response = await searchDeposits(params)
      const details = await Promise.all(response.items.map((item) => getDeposit(item.transactionId)))
      setDepositSearchResult(details)
    })
  }

  async function handleLoadPendingReview() {
    await runAction('Load pending review queue', async () => {
      const response = await getPendingReview(sortBy, descending)
      setPendingReviewItems(response.items)
    })
  }

  async function handleRetryPendingReview(transactionId: string) {
    await runAction('Retry compensation', async () => {
      setDeposit(
        await retryPendingReview(transactionId, 'frontend-ops', 'Retry requested from the React console.'),
      )
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
      await handleLoadPendingReview()
    })
  }

  return (
    <main className="app-shell">
      <section className="hero-panel">
        <div>
          <p className="eyebrow">Basic Banking System</p>
          <h1>Operations Console</h1>
          <p className="intro">
            A frontend-first operator workspace for customer onboarding, account opening,
            deposit submission, and pending-review recovery.
          </p>
        </div>
        <div className="status-strip">
          <button className="ghost-button" onClick={() => void loadHealth()}>
            Refresh health
          </button>
          <p>{message}</p>
        </div>
      </section>

      <section className="grid">
        <EnvironmentPanel health={health} />
        <CustomerPanel
          customer={customer}
          form={customerForm}
          onFormChange={setCustomerForm}
          onCreate={() => void handleCreateCustomer()}
          onActivate={() => void handleActivateCustomer()}
        />
        <AccountPanel
          account={account}
          onOpen={() => void handleOpenAccount()}
          onRefresh={() => void handleRefreshAccount()}
        />
        <DepositPanel
          deposit={deposit}
          form={depositForm}
          onFormChange={setDepositForm}
          onSubmit={() => void handleSubmitDeposit()}
          onRefresh={() => void handleRefreshDeposit()}
        />
      </section>

      <PendingReviewPanel
        sortBy={sortBy}
        descending={descending}
        pendingReviewItems={pendingReviewItems}
        depositSearchResult={depositSearchResult}
        onSortByChange={setSortBy}
        onDescendingChange={setDescending}
        onLoadQueue={() => void handleLoadPendingReview()}
        onSearchDeposits={() => void handleSearchDeposits()}
        onRetry={(transactionId) => void handleRetryPendingReview(transactionId)}
        onResolve={(transactionId, resolution) => void handleResolvePendingReview(transactionId, resolution)}
      />
    </main>
  )
}

export default App
