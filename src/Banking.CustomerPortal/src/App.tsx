import { useEffect, useState } from 'react'
import './App.css'
import { getAccount, getAccountActivities, getAccountsByCustomer, getDeposit, searchDeposits, signInCustomer, submitDeposit, submitWithdrawal } from './api'
import type {
  AccountActivityResponse,
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  DepositResponse,
} from './types'
import { formatCurrency } from './utils/currency'

type PortalTab = 'dashboard' | 'accounts' | 'activity' | 'cash' | 'transactions' | 'profile'
type CashTab = 'deposit' | 'withdraw'

function randomDigits(length: number) {
  return Array.from({ length }, () => Math.floor(Math.random() * 10).toString()).join('')
}

function buildPortalTransactionForm(mode: CashTab) {
  const prefix = mode === 'deposit' ? 'PORTAL-DEP' : 'PORTAL-WD'
  const note = mode === 'deposit'
    ? 'Submitted from the customer portal deposit workspace.'
    : 'Submitted from the customer portal withdrawal workspace.'

  return {
    amount: '100',
    referenceNumber: `${prefix}-${Date.now()}-${randomDigits(4)}`,
    note,
  }
}

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

function getDepositStatusTone(status: number) {
  switch (status) {
    case 3:
      return 'success'
    case 1:
    case 2:
      return 'info'
    case 6:
      return 'warning'
    case 4:
    case 5:
    case 7:
      return 'danger'
    default:
      return 'neutral'
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
  const [currentCustomer, setCurrentCustomer] = useState<CustomerResponse | null>(null)
  const [accounts, setAccounts] = useState<AccountSummaryResponse[]>([])
  const [selectedAccount, setSelectedAccount] = useState<AccountResponse | null>(null)
  const [activities, setActivities] = useState<AccountActivityResponse[]>([])
  const [latestDeposit, setLatestDeposit] = useState<DepositResponse | null>(null)
  const [depositStatuses, setDepositStatuses] = useState<DepositResponse[]>([])
  const [cashTab, setCashTab] = useState<CashTab>('deposit')
  const [transactionForm, setTransactionForm] = useState(() => buildPortalTransactionForm('deposit'))
  const [loginForm, setLoginForm] = useState({
    customerNumber: '',
    identityLast4: '',
  })
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('Enter your customer number and the last 4 digits of your identity number.')

  useEffect(() => {
    if (!currentCustomer) {
      setDepositStatuses([])
      return
    }

    // The portal keeps customer and account context local in the browser, then
    // refreshes the deposit list whenever that context changes.
    void loadDepositStatuses(currentCustomer.customerId, selectedAccount?.accountId)
  }, [currentCustomer?.customerId, selectedAccount?.accountId])

  useEffect(() => {
    setTransactionForm(buildPortalTransactionForm(cashTab))
  }, [cashTab])

  useEffect(() => {
    const pending = depositStatuses.some((item) => item.status === 1 || item.status === 2)
      || (latestDeposit !== null && (latestDeposit.status === 1 || latestDeposit.status === 2))

    if (!pending || !currentCustomer) {
      return
    }

    // Customer-facing polling only runs for transient states so the portal behaves
    // like a live transaction tracker without turning every screen into a polling client.
    const handle = window.setInterval(() => {
      void loadDepositStatuses(currentCustomer.customerId, selectedAccount?.accountId, true)
    }, 2000)

    return () => window.clearInterval(handle)
  }, [currentCustomer, selectedAccount, depositStatuses, latestDeposit])

  async function loadCustomerWorkspace(customer: CustomerResponse) {
    try {
      setBusy(true)
      setCurrentCustomer(customer)
      const accountResponse = await getAccountsByCustomer(customer.customerId)
      setAccounts(accountResponse.items)

      if (accountResponse.items.length > 0) {
        // The first account is loaded automatically so the portal feels task-ready
        // immediately after sign-in.
        await loadAccountWorkspace(accountResponse.items[0].accountId)
      } else {
        setSelectedAccount(null)
        setActivities([])
        setLatestDeposit(null)
        setDepositStatuses([])
      }

      setMessage(`Signed in as ${customer.fullName}.`)
    } catch (error) {
      setMessage(`Could not load customer workspace: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function handleSignIn() {
    const customerNumber = loginForm.customerNumber.trim()
    const identityLast4 = loginForm.identityLast4.trim()

    if (!customerNumber || identityLast4.length !== 4) {
      setMessage('Enter a customer number and the last 4 digits of your identity number.')
      return
    }

    try {
      setBusy(true)
      // The portal never inspects raw identity values from the customer list. Sign-in
      // goes through a dedicated backend check instead of leaking verification logic to the UI.
      const matched = await signInCustomer(customerNumber, identityLast4)
      await loadCustomerWorkspace(matched)
    } catch (error) {
      setMessage(`Sign-in failed: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  function handleSignOut() {
    setCurrentCustomer(null)
    setAccounts([])
    setSelectedAccount(null)
    setActivities([])
    setLatestDeposit(null)
    setDepositStatuses([])
    setActiveTab('dashboard')
    setLoginForm({
      customerNumber: '',
      identityLast4: '',
    })
    setMessage('Signed out. Enter your customer number and identity last 4 digits to sign in again.')
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

  async function loadDepositStatuses(customerId: string, accountId?: string, silent = false) {
    try {
      const response = await searchDeposits(customerId, accountId)
      const details = await Promise.all(response.items.map((item) => getDeposit(item.transactionId)))
      const ordered = details.sort((left, right) => new Date(right.requestedAt).getTime() - new Date(left.requestedAt).getTime())
      setDepositStatuses(ordered)

      if (latestDeposit) {
        const refreshedLatest = ordered.find((item) => item.transactionId === latestDeposit.transactionId)
        if (refreshedLatest) {
          setLatestDeposit(refreshedLatest)
        }
      } else if (ordered.length > 0) {
        setLatestDeposit(ordered[0])
      }

      if (!silent) {
        setMessage(`Loaded ${ordered.length} deposit transaction${ordered.length === 1 ? '' : 's'}.`)
      }
    } catch (error) {
      if (!silent) {
        setMessage(`Could not load deposit status: ${error instanceof Error ? error.message : String(error)}`)
      }
    }
  }

  async function handleSubmitDeposit() {
    if (!currentCustomer || !selectedAccount) {
      setMessage('Choose a customer and account first.')
      return
    }

    try {
      setBusy(true)
      // The portal reuses the same idempotent deposit contract as the operator console,
      // which keeps financial safety in the backend instead of forking client logic.
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
      await loadDepositStatuses(currentCustomer.customerId, selectedAccount.accountId, true)
      // After submission, the portal pivots to the transaction-status view because
      // that is the next decision point for the customer.
      setActiveTab('transactions')
      setMessage(`Deposit submitted. Current status: ${getDepositStatusLabel(result.status)}.`)
      setTransactionForm(buildPortalTransactionForm('deposit'))
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
      if (currentCustomer) {
        await loadDepositStatuses(currentCustomer.customerId, updated.accountId, true)
      }
      setMessage('Withdrawal submitted successfully.')
      setTransactionForm(buildPortalTransactionForm('withdraw'))
    } catch (error) {
      setMessage(`Could not submit withdrawal: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  if (!currentCustomer) {
    return (
      <main className="portal-shell">
        <section className="portal-hero portal-login-hero">
          <div>
            <p className="eyebrow">Basic Banking System</p>
            <h1>Customer Sign In</h1>
            <p className="intro">
              Use your customer number and the last 4 digits of your identity number to enter this local demo portal.
            </p>
          </div>
        </section>

        <section className="portal-login-shell">
          <article className="panel portal-login-card">
            <div className="panel-head">
              <div>
                <p className="eyebrow">Secure Access</p>
                <h2>Sign In</h2>
              </div>
            </div>

            <div className="form-grid">
              <label className="field-label">
                <span>Customer number</span>
                <input
                  value={loginForm.customerNumber}
                  onChange={(event) => setLoginForm({ ...loginForm, customerNumber: event.target.value })}
                  placeholder="Example: C202603..."
                  disabled={busy}
                />
              </label>
              <label className="field-label">
                <span>Identity number last 4 digits</span>
                <input
                  value={loginForm.identityLast4}
                  onChange={(event) => setLoginForm({ ...loginForm, identityLast4: event.target.value.slice(0, 4) })}
                  placeholder="Example: 1234"
                  maxLength={4}
                  disabled={busy}
                />
              </label>
            </div>

            <div className="action-row">
              <button type="button" onClick={() => void handleSignIn()} disabled={busy}>
                {busy ? 'Signing in...' : 'Sign in'}
              </button>
            </div>

            <p className="status-note">{message}</p>
          </article>

          <article className="panel portal-login-help">
            <p className="eyebrow">Demo Access Notes</p>
            <ul className="bullet-list">
              <li>This local demo sign-in does not replace real authentication.</li>
              <li>It validates your customer number and identity last 4 digits against the customer service.</li>
              <li>The next step after this demo is to replace it with customer-scoped authentication.</li>
            </ul>
          </article>
        </section>
      </main>
    )
  }

  return (
    <main className="portal-shell">
      <section className="portal-hero">
        <div>
          <p className="eyebrow">Basic Banking System</p>
          <h1>Customer Portal</h1>
          <p className="intro">
            A customer-facing self-service experience for balances, activity history, and profile information.
            The current sign-in flow is still a local demo credential check.
          </p>
        </div>
        <div className="hero-side">
          <div className="mini-detail-list">
            <div><dt>Signed in as</dt><dd>{currentCustomer.fullName}</dd></div>
            <div><dt>Customer number</dt><dd>{currentCustomer.customerNumber}</dd></div>
          </div>
          <div className="action-row">
            <button type="button" className="ghost-button" onClick={handleSignOut} disabled={busy}>Sign out</button>
          </div>
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
                ['transactions', 'Transactions', 'Track deposit processing status'],
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
            <dl className="mini-detail-list">
              <div><dt>Name</dt><dd>{currentCustomer.fullName}</dd></div>
              <div><dt>Customer number</dt><dd>{currentCustomer.customerNumber}</dd></div>
              <div><dt>Status</dt><dd>{getCustomerStatusLabel(currentCustomer.status)}</dd></div>
            </dl>
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
                  <h2>Cash Transactions</h2>
                </div>
              </div>

              <div className="portal-subtab-list">
                <button
                  type="button"
                  className={cashTab === 'deposit' ? 'portal-subtab portal-subtab-active' : 'portal-subtab'}
                  onClick={() => setCashTab('deposit')}
                >
                  Deposit
                </button>
                <button
                  type="button"
                  className={cashTab === 'withdraw' ? 'portal-subtab portal-subtab-active' : 'portal-subtab'}
                  onClick={() => setCashTab('withdraw')}
                >
                  Withdraw
                </button>
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
                  <p className="eyebrow">{cashTab === 'deposit' ? 'Deposit Form' : 'Withdrawal Form'}</p>
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
                    {cashTab === 'deposit' ? (
                      <button type="button" onClick={() => void handleSubmitDeposit()} disabled={!selectedAccount || busy}>
                        {busy ? 'Working...' : 'Submit deposit'}
                      </button>
                    ) : (
                      <button type="button" onClick={() => void handleSubmitWithdrawal()} disabled={!selectedAccount || busy}>
                        {busy ? 'Working...' : 'Submit withdrawal'}
                      </button>
                    )}
                    <button
                      type="button"
                      className="ghost-button"
                      onClick={() => setTransactionForm(buildPortalTransactionForm(cashTab))}
                      disabled={busy}
                    >
                      Generate demo values
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

          {activeTab === 'transactions' && (
            <article className="panel">
              <div className="panel-head">
                <div>
                  <p className="eyebrow">Transaction Status</p>
                  <h2>Deposit Progress</h2>
                  <p className="status-note">
                    Deposits update automatically while they are still being received or processed.
                  </p>
                </div>
                <div className="action-row">
                  <button
                    type="button"
                    className="ghost-button"
                    onClick={() => currentCustomer && void loadDepositStatuses(currentCustomer.customerId, selectedAccount?.accountId)}
                    disabled={!currentCustomer || busy}
                  >
                    Refresh statuses
                  </button>
                </div>
              </div>

              {depositStatuses.length === 0 ? (
                <p>No deposit transactions found for the current customer context yet.</p>
              ) : (
                <div className="transaction-status-list">
                  {depositStatuses.map((item) => (
                    <section key={item.transactionId} className="transaction-card">
                      <div className="transaction-card-head">
                        <div>
                          <p className="eyebrow">Transaction</p>
                          <h3>{item.transactionNumber}</h3>
                        </div>
                        <span className={`status-pill status-pill-${getDepositStatusTone(item.status)}`}>
                          {getDepositStatusLabel(item.status)}
                        </span>
                      </div>

                      <dl className="detail-list">
                        <div><dt>Account</dt><dd>{selectedAccount?.accountNumber ?? 'Selected account'}</dd></div>
                        <div><dt>Amount</dt><dd>{formatCurrency(item.amount, item.currency)}</dd></div>
                        <div><dt>Reference number</dt><dd>{item.referenceNumber ?? 'Not provided'}</dd></div>
                        <div><dt>Requested at</dt><dd>{new Date(item.requestedAt).toLocaleString()}</dd></div>
                        <div><dt>Posted at</dt><dd>{item.postedAt ? new Date(item.postedAt).toLocaleString() : 'Still pending'}</dd></div>
                        <div><dt>Failure reason</dt><dd>{item.failureReason ?? item.failureCode ?? 'No failure reported'}</dd></div>
                      </dl>

                      <div className="progress-strip">
                        <div className={item.status >= 1 ? 'progress-step progress-step-done' : 'progress-step'}>
                          <strong>Received</strong>
                          <span>The request reached the banking platform.</span>
                        </div>
                        <div className={item.status >= 2 ? 'progress-step progress-step-done' : 'progress-step'}>
                          <strong>Processing</strong>
                          <span>The platform is applying the deposit.</span>
                        </div>
                        <div className={item.status === 3 ? 'progress-step progress-step-done' : 'progress-step'}>
                          <strong>Succeeded</strong>
                          <span>Funds were posted successfully.</span>
                        </div>
                        <div className={item.status === 5 || item.status === 4 || item.status === 6 ? 'progress-step progress-step-alert' : 'progress-step'}>
                          <strong>Attention</strong>
                          <span>Failed, rejected, or pending review items appear here.</span>
                        </div>
                      </div>
                    </section>
                  ))}
                </div>
              )}
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
                      <div><dt>Email</dt><dd>{currentCustomer.email ?? 'Not provided'}</dd></div>
                      <div><dt>Identity type</dt><dd>{currentCustomer.identityType}</dd></div>
                      <div><dt>Identity number</dt><dd>{currentCustomer.identityNumberMasked}</dd></div>
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
