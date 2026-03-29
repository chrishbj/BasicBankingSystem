import { useEffect, useState } from 'react'
import './App.css'
import { getAccount, getAccountActivities, getAccountsByCustomer, getCustomers, submitDeposit, submitWithdrawal } from './api'
import type {
  AccountActivityResponse,
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  DepositResponse,
} from './types'
import { formatCurrency } from './utils/currency'

type PortalTab = 'dashboard' | 'accounts' | 'activity' | 'cash' | 'profile'

function getActivityLabel(postingType: AccountActivityResponse['postingType']) {
  switch (postingType) {
    case 1:
      return 'Deposit'
    case 2:
      return 'Deposit Reversal'
    case 3:
      return 'Withdrawal'
    default:
      return `Activity ${postingType}`
  }
}

function getAccountStatusLabel(status: number) {
  switch (status) {
    case 1:
      return 'Active'
    case 2:
      return 'Frozen'
    case 3:
      return 'Closed'
    default:
      return `Status ${status}`
  }
}

function getCustomerStatusLabel(status: number) {
  switch (status) {
    case 1:
      return 'Pending'
    case 2:
      return 'Active'
    case 3:
      return 'Frozen'
    default:
      return `Status ${status}`
  }
}

function getRiskLabel(riskLevel?: string) {
  if (!riskLevel) {
    return 'Standard'
  }

  return riskLevel
}

function getDepositStatusLabel(status: number) {
  switch (status) {
    case 1:
      return 'Received'
    case 2:
      return 'Processing'
    case 3:
      return 'Succeeded'
    case 4:
      return 'Rejected'
    case 5:
      return 'Failed'
    case 6:
      return 'Pending Review'
    case 7:
      return 'Reversed'
    default:
      return `Status ${status}`
  }
}

function groupActivitiesByDate(items: AccountActivityResponse[]) {
  const grouped = new Map<string, AccountActivityResponse[]>()

  items.forEach((item) => {
    const dateKey = new Date(item.createdAt).toLocaleDateString()
    const existing = grouped.get(dateKey) ?? []
    existing.push(item)
    grouped.set(dateKey, existing)
  })

  return Array.from(grouped.entries())
}

function App() {
  const [activeTab, setActiveTab] = useState<PortalTab>('dashboard')
  const [customers, setCustomers] = useState<CustomerResponse[]>([])
  const [currentCustomer, setCurrentCustomer] = useState<CustomerResponse | null>(null)
  const [accounts, setAccounts] = useState<AccountSummaryResponse[]>([])
  const [selectedAccount, setSelectedAccount] = useState<AccountResponse | null>(null)
  const [activities, setActivities] = useState<AccountActivityResponse[]>([])
  const [latestDeposit, setLatestDeposit] = useState<DepositResponse | null>(null)
  const [transactionForm, setTransactionForm] = useState({
    amount: '100',
    referenceNumber: `PORTAL-REF-${Date.now()}`,
    note: 'Submitted from the customer portal.',
  })
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('Pick a customer to enter the portal demo.')

  useEffect(() => {
    void loadCustomers()
  }, [])

  async function loadCustomers() {
    try {
      setBusy(true)
      const response = await getCustomers()
      setCustomers(response.items)

      if (!currentCustomer && response.items.length > 0) {
        await loadCustomerWorkspace(response.items[0])
      } else {
        setMessage('Customer directory loaded.')
      }
    } catch (error) {
      setMessage(`Could not load customers: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function loadCustomerWorkspace(customer: CustomerResponse) {
    try {
      setBusy(true)
      setCurrentCustomer(customer)
      const accountResponse = await getAccountsByCustomer(customer.customerId)
      setAccounts(accountResponse.items)

      if (accountResponse.items.length > 0) {
        await loadAccountWorkspace(accountResponse.items[0].accountId)
      } else {
        setSelectedAccount(null)
        setActivities([])
      }

      setMessage(`Signed in as ${customer.fullName}.`)
    } catch (error) {
      setMessage(`Could not load customer workspace: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function loadAccountWorkspace(accountId: string) {
    try {
      setBusy(true)
      const account = await getAccount(accountId)
      const activityResponse = await getAccountActivities(accountId)
      setSelectedAccount(account)
      setActivities(activityResponse.items)
      setMessage(`Loaded account ${account.accountNumber}.`)
    } catch (error) {
      setMessage(`Could not load account details: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  const totalAvailable = accounts.reduce((sum, item) => sum + item.availableBalance, 0)
  const totalLedger = accounts.reduce((sum, item) => sum + item.ledgerBalance, 0)
  const groupedActivities = groupActivitiesByDate(activities)

  async function refreshCurrentAccount() {
    if (!selectedAccount) {
      return
    }

    await loadAccountWorkspace(selectedAccount.accountId)
  }

  async function handleSubmitDeposit() {
    if (!currentCustomer || !selectedAccount) {
      setMessage('Choose a customer and account first.')
      return
    }

    try {
      setBusy(true)
      const result = await submitDeposit(
        {
          customerId: currentCustomer.customerId,
          accountId: selectedAccount.accountId,
          amount: Number(transactionForm.amount),
          currency: selectedAccount.currency,
          channel: 1,
          referenceNumber: transactionForm.referenceNumber.trim(),
          note: transactionForm.note.trim(),
        },
        `portal-idem-${Date.now()}`,
        `portal-corr-${Date.now()}`,
      )

      setLatestDeposit(result)
      await refreshCurrentAccount()
      setMessage(`Deposit submitted. Current status: ${getDepositStatusLabel(result.status)}.`)
    } catch (error) {
      setMessage(`Could not submit deposit: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function handleSubmitWithdrawal() {
    if (!selectedAccount) {
      setMessage('Choose an account first.')
      return
    }

    try {
      setBusy(true)
      const updated = await submitWithdrawal(selectedAccount.accountId, {
        amount: Number(transactionForm.amount),
        currency: selectedAccount.currency,
        referenceNumber: transactionForm.referenceNumber.trim(),
        correlationId: `portal-wd-corr-${Date.now()}`,
        note: transactionForm.note.trim(),
      })

      setSelectedAccount(updated)
      await refreshCurrentAccount()
      setMessage('Withdrawal submitted successfully.')
    } catch (error) {
      setMessage(`Could not submit withdrawal: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="portal-shell">
      <section className="portal-hero">
        <div>
          <p className="eyebrow">Basic Banking System</p>
          <h1>Customer Portal</h1>
          <p className="intro">
            A customer-facing self-service experience for balances, activity history, and profile information.
            This first version uses a demo customer selector in place of full authentication.
          </p>
        </div>
        <div className="hero-side">
          <label className="field-label">
            <span>Portal customer sign-in</span>
            <select
              value={currentCustomer?.customerId ?? ''}
              onChange={(event) => {
                const next = customers.find((item) => item.customerId === event.target.value)
                if (next) {
                  void loadCustomerWorkspace(next)
                }
              }}
              disabled={busy || customers.length === 0}
            >
              <option value="">Choose customer</option>
              {customers.map((item) => (
                <option key={item.customerId} value={item.customerId}>
                  {item.fullName} | {item.customerNumber}
                </option>
              ))}
            </select>
          </label>
          <p className="status-note">{message}</p>
        </div>
      </section>

      <section className="portal-layout">
        <aside className="portal-sidebar">
          <article className="panel compact-panel">
            <p className="eyebrow">Portal Menu</p>
            <div className="portal-tab-list">
              {[
                ['dashboard', 'Dashboard', 'Overview, balances, and alerts'],
                ['accounts', 'Accounts', 'Your account list and balances'],
                ['activity', 'Activity', 'Deposits and withdrawals history'],
                ['cash', 'Cash', 'Submit deposits and withdrawals'],
                ['profile', 'Profile', 'Customer details and contact info'],
              ].map(([id, label, hint]) => (
                <button
                  key={id}
                  type="button"
                  className={activeTab === id ? 'portal-tab portal-tab-active' : 'portal-tab'}
                  onClick={() => setActiveTab(id as PortalTab)}
                >
                  <strong>{label}</strong>
                  <span>{hint}</span>
                </button>
              ))}
            </div>
          </article>

          <article className="panel compact-panel">
            <p className="eyebrow">Current Customer</p>
            {currentCustomer ? (
              <dl className="mini-detail-list">
                <div><dt>Name</dt><dd>{currentCustomer.fullName}</dd></div>
                <div><dt>Customer number</dt><dd>{currentCustomer.customerNumber}</dd></div>
                <div><dt>Status</dt><dd>{getCustomerStatusLabel(currentCustomer.status)}</dd></div>
              </dl>
            ) : (
              <p>Select a customer to start the demo portal.</p>
            )}
          </article>
        </aside>

        <section className="portal-content">
          {activeTab === 'dashboard' && (
            <article className="panel">
              <div className="panel-head">
                <div>
                  <p className="eyebrow">Customer Snapshot</p>
                  <h2>Dashboard</h2>
                </div>
              </div>

              <div className="summary-grid">
                <section className="summary-card">
                  <p className="eyebrow">Portfolio</p>
                  <h3>{accounts.length} account{accounts.length === 1 ? '' : 's'}</h3>
                  <dl className="mini-detail-list">
                    <div><dt>Available total</dt><dd>{formatCurrency(totalAvailable)}</dd></div>
                    <div><dt>Ledger total</dt><dd>{formatCurrency(totalLedger)}</dd></div>
                  </dl>
                </section>

                <section className="summary-card">
                  <p className="eyebrow">Current Account</p>
                  <h3>{selectedAccount?.accountNumber ?? 'None selected'}</h3>
                  <dl className="mini-detail-list">
                    <div><dt>Status</dt><dd>{selectedAccount ? getAccountStatusLabel(selectedAccount.status) : 'N/A'}</dd></div>
                    <div><dt>Available</dt><dd>{selectedAccount ? formatCurrency(selectedAccount.availableBalance, selectedAccount.currency) : '$0.00'}</dd></div>
                  </dl>
                </section>

                <section className="summary-card">
                  <p className="eyebrow">Recent Activity</p>
                  <h3>{activities.length} item{activities.length === 1 ? '' : 's'}</h3>
                  <dl className="mini-detail-list">
                    <div><dt>Latest type</dt><dd>{activities[0] ? getActivityLabel(activities[0].postingType) : 'No activity yet'}</dd></div>
                    <div><dt>Latest date</dt><dd>{activities[0] ? new Date(activities[0].createdAt).toLocaleString() : 'N/A'}</dd></div>
                  </dl>
                </section>
              </div>

              <div className="content-grid">
                <section className="info-card">
                  <p className="eyebrow">Recent Activity Feed</p>
                  {activities.length === 0 ? (
                    <p>No activity available for the selected customer yet.</p>
                  ) : (
                    <ul className="timeline">
                      {activities.slice(0, 6).map((item) => (
                        <li key={item.postingReference}>
                          <div className="timeline-marker" />
                          <div>
                            <strong>{getActivityLabel(item.postingType)}</strong>
                            <span>{item.postingReference}</span>
                            <span>{new Date(item.createdAt).toLocaleString()}</span>
                          </div>
                          <strong>{formatCurrency(item.amount, item.currency)}</strong>
                        </li>
                      ))}
                    </ul>
                  )}
                </section>

                <section className="info-card">
                  <p className="eyebrow">What Comes Next</p>
                  <ul className="bullet-list">
                    <li>Add real customer authentication instead of the demo selector.</li>
                    <li>Introduce customer-safe transfer, deposit, and withdrawal journeys.</li>
                    <li>Add downloadable statements and notification preferences.</li>
                  </ul>
                </section>
              </div>
            </article>
          )}

          {activeTab === 'accounts' && (
            <article className="panel">
              <div className="panel-head">
                <div>
                  <p className="eyebrow">Accounts</p>
                  <h2>My Accounts</h2>
                </div>
              </div>

              {accounts.length === 0 ? (
                <p>No accounts found for this customer.</p>
              ) : (
                <div className="account-grid">
                  {accounts.map((item) => (
                    <button
                      key={item.accountId}
                      type="button"
                      className={selectedAccount?.accountId === item.accountId ? 'account-card account-card-active' : 'account-card'}
                      onClick={() => void loadAccountWorkspace(item.accountId)}
                    >
                      <div className="account-card-head">
                        <strong>{item.accountType}</strong>
                        <span>{getAccountStatusLabel(item.status)}</span>
                      </div>
                      <span>{item.accountNumber}</span>
                      <span>Available: {formatCurrency(item.availableBalance, item.currency)}</span>
                      <span>Ledger: {formatCurrency(item.ledgerBalance, item.currency)}</span>
                    </button>
                  ))}
                </div>
              )}
            </article>
          )}

          {activeTab === 'activity' && (
            <article className="panel">
              <div className="panel-head">
                <div>
                  <p className="eyebrow">History</p>
                  <h2>Account Activity</h2>
                </div>
              </div>

              {groupedActivities.length === 0 ? (
                <p>No activity found for the selected account.</p>
              ) : (
                <div className="activity-groups">
                  {groupedActivities.map(([date, items]) => (
                    <section key={date} className="activity-group">
                      <h3>{date}</h3>
                      <div className="activity-list">
                        {items.map((item) => (
                          <div key={item.postingReference} className="activity-row">
                            <div>
                              <strong>{getActivityLabel(item.postingType)}</strong>
                              <span>{item.postingReference}</span>
                              <span>{item.correlationId ?? 'No correlation reference'}</span>
                            </div>
                            <div className="activity-amount">
                              <strong>{formatCurrency(item.amount, item.currency)}</strong>
                              <span>{new Date(item.createdAt).toLocaleTimeString()}</span>
                            </div>
                          </div>
                        ))}
                      </div>
                    </section>
                  ))}
                </div>
              )}
            </article>
          )}

          {activeTab === 'cash' && (
            <article className="panel">
              <div className="panel-head">
                <div>
                  <p className="eyebrow">Cash Services</p>
                  <h2>Deposit Or Withdraw</h2>
                </div>
              </div>

              <div className="content-grid">
                <section className="info-card">
                  <p className="eyebrow">Current Account</p>
                  {selectedAccount ? (
                    <dl className="detail-list">
                      <div><dt>Account number</dt><dd>{selectedAccount.accountNumber}</dd></div>
                      <div><dt>Available balance</dt><dd>{formatCurrency(selectedAccount.availableBalance, selectedAccount.currency)}</dd></div>
                      <div><dt>Status</dt><dd>{getAccountStatusLabel(selectedAccount.status)}</dd></div>
                    </dl>
                  ) : (
                    <p>Select an account first from the Accounts tab.</p>
                  )}
                </section>

                <section className="info-card">
                  <p className="eyebrow">Transaction Form</p>
                  <div className="form-grid">
                    <label className="field-label">
                      <span>Amount</span>
                      <input
                        value={transactionForm.amount}
                        onChange={(event) => setTransactionForm({ ...transactionForm, amount: event.target.value })}
                        disabled={busy}
                      />
                    </label>
                    <label className="field-label">
                      <span>Reference number</span>
                      <input
                        value={transactionForm.referenceNumber}
                        onChange={(event) => setTransactionForm({ ...transactionForm, referenceNumber: event.target.value })}
                        disabled={busy}
                      />
                    </label>
                    <label className="field-label">
                      <span>Note</span>
                      <textarea
                        value={transactionForm.note}
                        onChange={(event) => setTransactionForm({ ...transactionForm, note: event.target.value })}
                        rows={4}
                        disabled={busy}
                      />
                    </label>
                  </div>
                  <div className="action-row">
                    <button type="button" onClick={() => void handleSubmitDeposit()} disabled={!selectedAccount || busy}>
                      {busy ? 'Working...' : 'Submit deposit'}
                    </button>
                    <button type="button" className="ghost-button" onClick={() => void handleSubmitWithdrawal()} disabled={!selectedAccount || busy}>
                      {busy ? 'Working...' : 'Submit withdrawal'}
                    </button>
                  </div>
                </section>

                <section className="info-card">
                  <p className="eyebrow">Latest Deposit</p>
                  {latestDeposit ? (
                    <dl className="detail-list">
                      <div><dt>Transaction number</dt><dd>{latestDeposit.transactionNumber}</dd></div>
                      <div><dt>Status</dt><dd>{getDepositStatusLabel(latestDeposit.status)}</dd></div>
                      <div><dt>Amount</dt><dd>{formatCurrency(latestDeposit.amount, latestDeposit.currency)}</dd></div>
                      <div><dt>Reference number</dt><dd>{latestDeposit.referenceNumber ?? 'N/A'}</dd></div>
                    </dl>
                  ) : (
                    <p>No deposit submitted from the portal in this session yet.</p>
                  )}
                </section>
              </div>
            </article>
          )}

          {activeTab === 'profile' && (
            <article className="panel">
              <div className="panel-head">
                <div>
                  <p className="eyebrow">Identity</p>
                  <h2>Customer Profile</h2>
                </div>
              </div>

              {currentCustomer ? (
                <div className="profile-grid">
                  <section className="info-card">
                    <p className="eyebrow">Basic</p>
                    <dl className="detail-list">
                      <div><dt>Full name</dt><dd>{currentCustomer.fullName}</dd></div>
                      <div><dt>Customer number</dt><dd>{currentCustomer.customerNumber}</dd></div>
                      <div><dt>Status</dt><dd>{getCustomerStatusLabel(currentCustomer.status)}</dd></div>
                      <div><dt>Risk level</dt><dd>{getRiskLabel(currentCustomer.riskLevel)}</dd></div>
                    </dl>
                  </section>

                  <section className="info-card">
                    <p className="eyebrow">Contact</p>
                    <dl className="detail-list">
                      <div><dt>Mobile</dt><dd>{currentCustomer.mobile}</dd></div>
                      <div><dt>Email</dt><dd>{currentCustomer.email}</dd></div>
                      <div><dt>Identity type</dt><dd>{currentCustomer.identityType}</dd></div>
                      <div><dt>Identity number</dt><dd>{currentCustomer.identityNumber}</dd></div>
                    </dl>
                  </section>
                </div>
              ) : (
                <p>Choose a customer to see profile details.</p>
              )}
            </article>
          )}
        </section>
      </section>
    </main>
  )
}

export default App
