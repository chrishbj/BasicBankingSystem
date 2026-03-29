import type { AccountActivityResponse, AccountResponse, AccountSummaryResponse } from '../types'
import type { StatusTone } from './StatusBadge'
import { SectionStatus } from './SectionStatus'
import { StatusBadge, getAccountActivityLabel, getAccountActivityTone } from './StatusBadge'

type AccountHistoryFilterState = {
  activityType: string
  from: string
  to: string
}

type AccountPanelProps = {
  customerName?: string
  customerNumber?: string
  account: AccountResponse | null
  accountList: AccountSummaryResponse[]
  lookupAccountId: string
  historyStatusText: string
  history: AccountActivityResponse[]
  selectedHistoryItem: AccountActivityResponse | null
  historyFilters: AccountHistoryFilterState
  openDisabled: boolean
  lookupDisabled: boolean
  loadHistoryDisabled: boolean
  busy: boolean
  onLookupAccountIdChange: (accountId: string) => void
  onHistoryFiltersChange: (next: AccountHistoryFilterState) => void
  onOpen: () => void
  onRefresh: () => void
  onLookup: () => void
  onLoadHistory: () => void
  onSelectHistoryItem: (transaction: AccountActivityResponse) => void
  onLoadCustomerAccounts: () => void
  onSelectAccount: (accountId: string) => void
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

function getAccountStatusTone(status: number): StatusTone {
  switch (status) {
    case 1:
      return 'success'
    case 2:
    case 3:
      return 'danger'
    default:
      return 'neutral'
  }
}

export function AccountPanel({
  customerName,
  customerNumber,
  account,
  accountList,
  lookupAccountId,
  historyStatusText,
  history,
  selectedHistoryItem,
  historyFilters,
  openDisabled,
  lookupDisabled,
  loadHistoryDisabled,
  busy,
  onLookupAccountIdChange,
  onHistoryFiltersChange,
  onOpen,
  onRefresh,
  onLookup,
  onLoadHistory,
  onSelectHistoryItem,
  onLoadCustomerAccounts,
  onSelectAccount,
}: AccountPanelProps) {
  return (
    <article className="panel wide-panel">
      <div className="panel-head">
        <div>
          <p className="eyebrow">Account Workspace</p>
          <h2>Account Information</h2>
          <SectionStatus text={historyStatusText} />
        </div>
        <div className="actions">
          <button onClick={onOpen} disabled={openDisabled}>{busy ? 'Working...' : 'Open active checking account'}</button>
          <button className="ghost-button" onClick={onRefresh} disabled={!account || busy}>Refresh current</button>
        </div>
      </div>

      <div className="info-card">
        <p className="eyebrow">How To Use</p>
        <p>
          Select a customer first, load that customer&apos;s accounts, then click an account card to inspect balances and full activity history. New accounts are created directly in the Active state.
        </p>
      </div>

      <div className="search-grid account-search-grid">
        <label className="field-label">
          <span>Lookup internal account reference</span>
          <input
            value={lookupAccountId}
            onChange={(event) => onLookupAccountIdChange(event.target.value)}
            placeholder="Internal account reference"
            disabled={busy}
          />
        </label>
        <button className="ghost-button" onClick={onLookup} disabled={lookupDisabled}>
          {busy ? 'Working...' : 'Lookup account'}
        </button>
        <button className="ghost-button" onClick={onLoadHistory} disabled={loadHistoryDisabled}>
          {busy ? 'Working...' : 'Load activity history'}
        </button>
      </div>

      <div className="search-grid account-filter-grid">
        <label className="field-label">
          <span>Activity type</span>
          <select
            value={historyFilters.activityType}
            onChange={(event) => onHistoryFiltersChange({ ...historyFilters, activityType: event.target.value })}
            disabled={busy}
          >
            <option value="">Any activity type</option>
            <option value="DepositCredit">Deposit</option>
            <option value="WithdrawalDebit">Withdrawal</option>
            <option value="DepositReversal">Deposit Reversal</option>
          </select>
        </label>
        <label className="field-label">
          <span>From</span>
          <input
            type="datetime-local"
            value={historyFilters.from}
            onChange={(event) => onHistoryFiltersChange({ ...historyFilters, from: event.target.value })}
            disabled={busy}
          />
        </label>
        <label className="field-label">
          <span>To</span>
          <input
            type="datetime-local"
            value={historyFilters.to}
            onChange={(event) => onHistoryFiltersChange({ ...historyFilters, to: event.target.value })}
            disabled={busy}
          />
        </label>
      </div>

      <div className="actions">
        {customerName && <span className="helper-chip">Customer: {customerName}{customerNumber ? ` | ${customerNumber}` : ''}</span>}
        {account && <span className="helper-chip">Account: {account.accountNumber}</span>}
        <button className="ghost-button" onClick={onLoadCustomerAccounts} disabled={!customerName || busy}>
          {busy ? 'Working...' : 'Load customer accounts'}
        </button>
      </div>

      {accountList.length > 0 && (
        <div className="account-summary-grid">
          {accountList.map((item) => (
            <button
              key={item.accountId}
              type="button"
              className={item.accountId === account?.accountId ? 'account-summary-card account-summary-card-active' : 'account-summary-card'}
              onClick={() => onSelectAccount(item.accountId)}
            >
              <div className="account-summary-card-head">
                <strong>{item.accountType}</strong>
                <StatusBadge label={getAccountStatusLabel(item.status)} tone={getAccountStatusTone(item.status)} />
              </div>
              <span>{item.accountNumber}</span>
              <span>{item.availableBalance.toFixed(2)} / {item.ledgerBalance.toFixed(2)} {item.currency}</span>
              <span className="subtle-code">{item.accountId}</span>
            </button>
          ))}
        </div>
      )}

      {account && (
        <dl className="detail-list">
          <div><dt>Customer</dt><dd>{customerName ?? 'Selected customer'}</dd></div>
          {customerNumber && <div><dt>Customer Number</dt><dd>{customerNumber}</dd></div>}
          <div><dt>Account Number</dt><dd>{account.accountNumber}</dd></div>
          <div><dt>Status</dt><dd>{getAccountStatusLabel(account.status)}</dd></div>
          <div><dt>Type</dt><dd>{account.accountType}</dd></div>
          <div><dt>Currency</dt><dd>{account.currency}</dd></div>
          <div><dt>Available</dt><dd>{account.availableBalance.toFixed(2)}</dd></div>
          <div><dt>Ledger</dt><dd>{account.ledgerBalance.toFixed(2)}</dd></div>
          <div><dt>Opened At</dt><dd>{new Date(account.openedAt).toLocaleString()}</dd></div>
          <div><dt>Internal Reference</dt><dd className="subtle-code">{account.accountId}</dd></div>
        </dl>
      )}

      {history.length > 0 && (
        <div className="account-history-layout">
          <div className="table-scroll account-history-table">
            <table>
              <thead>
                <tr>
                  <th>Reference</th>
                  <th>Type</th>
                  <th>Amount</th>
                  <th>When</th>
                  <th>Correlation</th>
                </tr>
              </thead>
              <tbody>
                {history.map((item) => (
                  <tr
                    key={item.postingReference}
                    className={selectedHistoryItem?.postingReference === item.postingReference ? 'table-row-selected' : ''}
                    onClick={() => onSelectHistoryItem(item)}
                  >
                    <td>
                      <strong>{item.postingReference}</strong>
                      {item.reversalOfPostingReference && <span>Of {item.reversalOfPostingReference}</span>}
                    </td>
                    <td>
                      <StatusBadge label={getAccountActivityLabel(item.postingType)} tone={getAccountActivityTone(item.postingType)} />
                    </td>
                    <td>
                      <strong>{item.amount.toFixed(2)} {item.currency}</strong>
                    </td>
                    <td>{new Date(item.createdAt).toLocaleString()}</td>
                    <td>{item.correlationId ?? 'N/A'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {selectedHistoryItem && (
            <aside className="history-detail-card">
              <p className="eyebrow">Activity Detail</p>
              <h3>{selectedHistoryItem.postingReference}</h3>
              <dl className="detail-list">
                <div><dt>Reference Number</dt><dd>{selectedHistoryItem.postingReference}</dd></div>
                <div>
                  <dt>Activity Type</dt>
                  <dd>
                    <StatusBadge
                      label={getAccountActivityLabel(selectedHistoryItem.postingType)}
                      tone={getAccountActivityTone(selectedHistoryItem.postingType)}
                    />
                  </dd>
                </div>
                <div><dt>Amount</dt><dd>{selectedHistoryItem.amount.toFixed(2)} {selectedHistoryItem.currency}</dd></div>
                <div><dt>Occurred At</dt><dd>{new Date(selectedHistoryItem.createdAt).toLocaleString()}</dd></div>
                <div><dt>Correlation ID</dt><dd className="subtle-code">{selectedHistoryItem.correlationId ?? 'N/A'}</dd></div>
                {selectedHistoryItem.reversalOfPostingReference && <div><dt>Reversal Of</dt><dd className="subtle-code">{selectedHistoryItem.reversalOfPostingReference}</dd></div>}
              </dl>
            </aside>
          )}
        </div>
      )}
    </article>
  )
}
