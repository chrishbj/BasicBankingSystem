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
        <article className="panel">
          <h2>Environment</h2>
          <div className="health-grid">
            {Object.entries(health).map(([name, value]) => (
              <div key={name} className="health-card">
                <span>{name}</span>
                <strong>{value}</strong>
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <h2>Customer</h2>
          <div className="form-grid">
            <input value={customerForm.fullName} onChange={(event) => setCustomerForm((state) => ({ ...state, fullName: event.target.value }))} placeholder="Full name" />
            <input value={customerForm.identityNumber} onChange={(event) => setCustomerForm((state) => ({ ...state, identityNumber: event.target.value }))} placeholder="Identity number" />
            <input value={customerForm.mobile} onChange={(event) => setCustomerForm((state) => ({ ...state, mobile: event.target.value }))} placeholder="Mobile" />
            <input value={customerForm.email} onChange={(event) => setCustomerForm((state) => ({ ...state, email: event.target.value }))} placeholder="Email" />
          </div>
          <div className="actions">
            <button onClick={() => void handleCreateCustomer()}>Create customer</button>
            <button className="ghost-button" onClick={() => void handleActivateCustomer()}>Activate</button>
          </div>
          {customer && (
            <dl className="detail-list">
              <div><dt>ID</dt><dd>{customer.customerId}</dd></div>
              <div><dt>Number</dt><dd>{customer.customerNumber}</dd></div>
              <div><dt>Status</dt><dd>{customer.status}</dd></div>
            </dl>
          )}
        </article>

        <article className="panel">
          <h2>Account</h2>
          <div className="actions">
            <button onClick={() => void handleOpenAccount()}>Open checking account</button>
            <button className="ghost-button" onClick={() => void handleRefreshAccount()}>Refresh</button>
          </div>
          {account && (
            <dl className="detail-list">
              <div><dt>Account ID</dt><dd>{account.accountId}</dd></div>
              <div><dt>Currency</dt><dd>{account.currency}</dd></div>
              <div><dt>Available</dt><dd>{account.availableBalance.toFixed(2)}</dd></div>
              <div><dt>Ledger</dt><dd>{account.ledgerBalance.toFixed(2)}</dd></div>
            </dl>
          )}
        </article>

        <article className="panel">
          <h2>Deposit</h2>
          <div className="form-grid">
            <input value={depositForm.amount} onChange={(event) => setDepositForm((state) => ({ ...state, amount: event.target.value }))} placeholder="Amount" />
            <input value={depositForm.referenceNumber} onChange={(event) => setDepositForm((state) => ({ ...state, referenceNumber: event.target.value }))} placeholder="Reference number" />
            <textarea value={depositForm.note} onChange={(event) => setDepositForm((state) => ({ ...state, note: event.target.value }))} placeholder="Deposit note" rows={3} />
          </div>
          <div className="actions">
            <button onClick={() => void handleSubmitDeposit()}>Submit deposit</button>
            <button className="ghost-button" onClick={() => void handleRefreshDeposit()}>Refresh transaction</button>
          </div>
          {deposit && (
            <dl className="detail-list">
              <div><dt>Transaction</dt><dd>{deposit.transactionId}</dd></div>
              <div><dt>Status</dt><dd>{deposit.status}</dd></div>
              <div><dt>Correlation</dt><dd>{deposit.correlationId}</dd></div>
              <div><dt>Failure</dt><dd>{deposit.failureCode ?? 'None'}</dd></div>
            </dl>
          )}
        </article>
      </section>

      <section className="panel wide-panel">
        <div className="panel-head">
          <div>
            <p className="eyebrow">Operations Search</p>
            <h2>Pending Review Queue</h2>
          </div>
          <div className="toolbar">
            <select value={sortBy} onChange={(event) => setSortBy(event.target.value as PendingReviewSortBy)}>
              <option value="ReviewRequiredAt">Review required</option>
              <option value="LastCompensationAttemptAt">Last compensation attempt</option>
              <option value="RequestedAt">Requested at</option>
            </select>
            <label className="toggle">
              <input type="checkbox" checked={descending} onChange={(event) => setDescending(event.target.checked)} />
              Desc
            </label>
            <button onClick={() => void handleLoadPendingReview()}>Load queue</button>
            <button className="ghost-button" onClick={() => void handleSearchDeposits()}>Search matching deposits</button>
          </div>
        </div>

        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Transaction</th>
                <th>Failure</th>
                <th>Retries</th>
                <th>Review required</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {pendingReviewItems.map((item) => (
                <tr key={item.transactionId}>
                  <td>
                    <strong>{item.transactionId}</strong>
                    <span>{item.customerId}</span>
                  </td>
                  <td>{item.failureCode ?? 'N/A'}</td>
                  <td>{item.compensationRetryCount}</td>
                  <td>{item.reviewRequiredAt ?? item.requestedAt}</td>
                  <td className="table-actions">
                    <button className="tiny-button" onClick={() => void handleRetryPendingReview(item.transactionId)}>Retry</button>
                    <button className="tiny-button ghost-button" onClick={() => void handleResolvePendingReview(item.transactionId, 3)}>Mark reversed</button>
                    <button className="tiny-button ghost-button" onClick={() => void handleResolvePendingReview(item.transactionId, 4)}>Mark failed</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {depositSearchResult.length > 0 && (
          <div className="search-results">
            <h3>Latest filtered deposits</h3>
            <ul>
              {depositSearchResult.map((item) => (
                <li key={item.transactionId}>
                  <strong>{item.transactionId}</strong>
                  <span>{item.status}</span>
                  <span>{item.correlationId}</span>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>
    </main>
  )
}

export default App
