import type {
  AccountActivityResponse,
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  PendingReviewDepositSummaryResponse,
} from '../types'
import { formatCurrency } from '../utils/currency'
import {
  StatusBadge,
  getAccountActivityLabel,
  getAccountActivityTone,
} from './StatusBadge'

type WorkspaceTab = 'overview' | 'customer' | 'account' | 'deposit' | 'withdraw' | 'review'

type OverviewPanelProps = {
  customer: CustomerResponse | null
  customers: CustomerResponse[]
  account: AccountResponse | null
  accountList: AccountSummaryResponse[]
  accountHistory: AccountActivityResponse[]
  customerActivitySnapshot: AccountActivityResponse[]
  pendingReviewItems: PendingReviewDepositSummaryResponse[]
  onNavigate: (tab: WorkspaceTab) => void
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

function getCustomerStatusTone(status: number) {
  switch (status) {
    case 2:
      return 'success' as const
    case 3:
      return 'danger' as const
    default:
      return 'warning' as const
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

function getAccountStatusTone(status: number) {
  switch (status) {
    case 1:
      return 'success' as const
    case 2:
    case 3:
      return 'danger' as const
    default:
      return 'neutral' as const
  }
}

export function OverviewPanel({
  customer,
  customers,
  account,
  accountList,
  accountHistory,
  customerActivitySnapshot,
  pendingReviewItems,
  onNavigate,
}: OverviewPanelProps) {
  const totalAvailableBalance = accountList.reduce((sum, item) => sum + item.availableBalance, 0)
  const totalLedgerBalance = accountList.reduce((sum, item) => sum + item.ledgerBalance, 0)
  const customerPendingReviewCount = customer
    ? pendingReviewItems.filter((item) => item.customerId === customer.customerId).length
    : 0
  const accountPendingReviewCount = account
    ? pendingReviewItems.filter((item) => item.accountId === account.accountId).length
    : 0
  const depositCount = accountHistory.filter((item) => item.postingType === 1).length
  const withdrawalCount = accountHistory.filter((item) => item.postingType === 3).length
  const latestActivity = accountHistory[0] ?? null
  const reviewBreakdown = pendingReviewItems.reduce<Record<string, number>>((result, item) => {
    const key = item.failureCode?.trim() || 'Unspecified'
    result[key] = (result[key] ?? 0) + 1
    return result
  }, {})
  const topReviewReasons = Object.entries(reviewBreakdown)
    .sort((left, right) => right[1] - left[1])
    .slice(0, 4)

  return (
    <article className="panel wide-panel">
      <div className="panel-head">
        <div>
          <p className="eyebrow">Operations Overview</p>
          <h2>Dashboard</h2>
          <p className="section-status">
            Start here to inspect the selected customer, current account, and pending review pressure before drilling into workflow tabs.
          </p>
        </div>
        <div className="actions">
          <button className="ghost-button" type="button" onClick={() => onNavigate('customer')}>Manage customers</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('account')}>Open accounts</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('deposit')}>Submit deposit</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('review')}>Open review queue</button>
        </div>
      </div>

      <div className="overview-grid">
        <section className="overview-card">
          <div className="overview-card-head">
            <div>
              <p className="eyebrow">Customer Overview</p>
              <h3>{customer?.fullName ?? 'No customer selected'}</h3>
            </div>
            {customer && (
              <StatusBadge
                label={getCustomerStatusLabel(customer.status)}
                tone={getCustomerStatusTone(customer.status)}
              />
            )}
          </div>
          <dl className="overview-stat-grid">
            <div>
              <dt>Customer number</dt>
              <dd>{customer?.customerNumber ?? 'Select a customer'}</dd>
            </div>
            <div>
              <dt>Customers loaded</dt>
              <dd>{customers.length}</dd>
            </div>
            <div>
              <dt>Accounts loaded</dt>
              <dd>{accountList.length}</dd>
            </div>
            <div>
              <dt>Total available balance</dt>
              <dd>{accountList.length > 0 ? formatCurrency(totalAvailableBalance, accountList[0]?.currency ?? 'USD') : '$0.00'}</dd>
            </div>
            <div>
              <dt>Total ledger balance</dt>
              <dd>{accountList.length > 0 ? formatCurrency(totalLedgerBalance, accountList[0]?.currency ?? 'USD') : '$0.00'}</dd>
            </div>
            <div>
              <dt>Pending review</dt>
              <dd>{customerPendingReviewCount}</dd>
            </div>
          </dl>
          <div className="overview-foot">
            <span className="helper-chip">
              {customer ? 'Use Customer tab to browse or switch customers.' : 'Select a customer to hydrate account and activity data.'}
            </span>
          </div>
        </section>

        <section className="overview-card">
          <div className="overview-card-head">
            <div>
              <p className="eyebrow">Account Overview</p>
              <h3>{account?.accountNumber ?? 'No account selected'}</h3>
            </div>
            {account && (
              <StatusBadge
                label={getAccountStatusLabel(account.status)}
                tone={getAccountStatusTone(account.status)}
              />
            )}
          </div>
          <dl className="overview-stat-grid">
            <div>
              <dt>Available balance</dt>
              <dd>{account ? formatCurrency(account.availableBalance, account.currency) : 'Load an account'}</dd>
            </div>
            <div>
              <dt>Ledger balance</dt>
              <dd>{account ? formatCurrency(account.ledgerBalance, account.currency) : 'Load an account'}</dd>
            </div>
            <div>
              <dt>Deposits in loaded history</dt>
              <dd>{depositCount}</dd>
            </div>
            <div>
              <dt>Withdrawals in loaded history</dt>
              <dd>{withdrawalCount}</dd>
            </div>
          </dl>
          <div className="overview-foot">
            <span className="helper-chip">
              {account ? `Current account is ${account.accountType}.` : 'Use Account tab to open or look up an account by account number.'}
            </span>
          </div>
        </section>

        <section className="overview-card">
          <div className="overview-card-head">
            <div>
              <p className="eyebrow">Review Summary</p>
              <h3>{pendingReviewItems.length} pending item{pendingReviewItems.length === 1 ? '' : 's'}</h3>
            </div>
            <StatusBadge
              label={pendingReviewItems.length > 0 ? 'Needs attention' : 'Clear'}
              tone={pendingReviewItems.length > 0 ? 'warning' : 'success'}
            />
          </div>
          <dl className="overview-stat-grid">
            <div>
              <dt>For selected customer</dt>
              <dd>{customerPendingReviewCount}</dd>
            </div>
            <div>
              <dt>For selected account</dt>
              <dd>{accountPendingReviewCount}</dd>
            </div>
            <div>
              <dt>Latest activity</dt>
              <dd>{latestActivity ? new Date(latestActivity.createdAt).toLocaleString() : 'No activity loaded'}</dd>
            </div>
            <div>
              <dt>Latest activity type</dt>
              <dd>{latestActivity ? getAccountActivityLabel(latestActivity.postingType) : 'N/A'}</dd>
            </div>
          </dl>
          <div className="overview-foot">
            {latestActivity ? (
              <span className="helper-chip">
                Latest activity:
                {' '}
                <StatusBadge
                  label={getAccountActivityLabel(latestActivity.postingType)}
                  tone={getAccountActivityTone(latestActivity.postingType)}
                />
              </span>
            ) : (
              <span className="helper-chip">Load account history to unlock richer account and review insight.</span>
            )}
          </div>
        </section>
      </div>

      <div className="overview-bottom-grid">
        <section className="info-card">
          <p className="eyebrow">Recent Activity</p>
          {customerActivitySnapshot.length === 0 ? (
            <p>No customer activity snapshot is loaded yet. Select a customer to load related accounts and recent deposits or withdrawals here.</p>
          ) : (
            <ul className="overview-list">
              {customerActivitySnapshot.slice(0, 6).map((item) => (
                <li key={item.postingReference}>
                  <div>
                    <strong>{item.postingReference}</strong>
                    <span>{new Date(item.createdAt).toLocaleString()}</span>
                  </div>
                  <div className="overview-list-meta">
                    <StatusBadge label={getAccountActivityLabel(item.postingType)} tone={getAccountActivityTone(item.postingType)} />
                    <span>{formatCurrency(item.amount, item.currency)}</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="info-card">
          <p className="eyebrow">Pending Review Snapshot</p>
          {pendingReviewItems.length === 0 ? (
            <p>The queue is currently empty. If you need a test case, go to Review and create a demo review item for the selected account.</p>
          ) : (
            <ul className="overview-list">
              {pendingReviewItems.slice(0, 5).map((item) => (
                <li key={item.transactionId}>
                  <div>
                    <strong>{item.transactionNumber}</strong>
                    <span>{item.failureCode ?? 'No failure code'}</span>
                  </div>
                  <div className="overview-list-meta">
                    <span>{formatCurrency(item.amount, item.currency)}</span>
                    <span>{item.compensationRetryCount} retries</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="info-card">
          <p className="eyebrow">Review Reason Breakdown</p>
          {topReviewReasons.length === 0 ? (
            <p>No failure reasons are currently present in the pending review queue.</p>
          ) : (
            <ul className="overview-list">
              {topReviewReasons.map(([reason, count]) => (
                <li key={reason}>
                  <div>
                    <strong>{reason}</strong>
                    <span>Pending review failure code</span>
                  </div>
                  <div className="overview-list-meta">
                    <StatusBadge label={`${count} item${count === 1 ? '' : 's'}`} tone={count > 1 ? 'warning' : 'info'} />
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </article>
  )
}
