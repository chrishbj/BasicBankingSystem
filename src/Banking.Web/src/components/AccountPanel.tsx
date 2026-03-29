import type { AccountResponse, DepositSummaryResponse } from '../types'
import { SectionStatus } from './SectionStatus'
import { StatusBadge, getDepositStatusLabel, getDepositStatusTone } from './StatusBadge'

type AccountPanelProps = {
  customerId?: string
  account: AccountResponse | null
  lookupAccountId: string
  historyStatusText: string
  history: DepositSummaryResponse[]
  openDisabled: boolean
  lookupDisabled: boolean
  busy: boolean
  onLookupAccountIdChange: (accountId: string) => void
  onOpen: () => void
  onRefresh: () => void
  onLookup: () => void
  onLoadHistory: () => void
}

function getAccountStatusLabel(status: number) {
  switch (status) {
    case 1:
      return 'Pending Activation'
    case 2:
      return 'Active'
    case 3:
      return 'Frozen'
    case 4:
      return 'Closed'
    default:
      return `Status ${status}`
  }
}

export function AccountPanel({
  customerId,
  account,
  lookupAccountId,
  historyStatusText,
  history,
  openDisabled,
  lookupDisabled,
  busy,
  onLookupAccountIdChange,
  onOpen,
  onRefresh,
  onLookup,
  onLoadHistory,
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
          <button onClick={onOpen} disabled={openDisabled}>{busy ? 'Working...' : 'Open checking account'}</button>
          <button className="ghost-button" onClick={onRefresh} disabled={!account || busy}>Refresh current</button>
        </div>
      </div>

      <div className="search-grid account-search-grid">
        <input
          value={lookupAccountId}
          onChange={(event) => onLookupAccountIdChange(event.target.value)}
          placeholder="Account ID"
          disabled={busy}
        />
        <button className="ghost-button" onClick={onLookup} disabled={lookupDisabled}>
          {busy ? 'Working...' : 'Lookup account'}
        </button>
        <button className="ghost-button" onClick={onLoadHistory} disabled={lookupDisabled}>
          {busy ? 'Working...' : 'Load history'}
        </button>
      </div>

      <div className="actions">
        {customerId && <span className="helper-chip">Active customer: {customerId}</span>}
        {account && <span className="helper-chip">Selected account: {account.accountId}</span>}
      </div>

      {account && (
        <dl className="detail-list">
          <div><dt>Customer ID</dt><dd>{account.customerId}</dd></div>
          <div><dt>Account ID</dt><dd>{account.accountId}</dd></div>
          <div><dt>Account Number</dt><dd>{account.accountNumber}</dd></div>
          <div><dt>Status</dt><dd>{getAccountStatusLabel(account.status)}</dd></div>
          <div><dt>Type</dt><dd>{account.accountType}</dd></div>
          <div><dt>Currency</dt><dd>{account.currency}</dd></div>
          <div><dt>Available</dt><dd>{account.availableBalance.toFixed(2)}</dd></div>
          <div><dt>Ledger</dt><dd>{account.ledgerBalance.toFixed(2)}</dd></div>
          <div><dt>Opened At</dt><dd>{new Date(account.openedAt).toLocaleString()}</dd></div>
        </dl>
      )}

      {history.length > 0 && (
        <div className="table-scroll account-history-table">
          <table>
            <thead>
              <tr>
                <th>Transaction</th>
                <th>Status</th>
                <th>Amount</th>
                <th>Requested At</th>
                <th>Posted At</th>
              </tr>
            </thead>
            <tbody>
              {history.map((item) => (
                <tr key={item.transactionId}>
                  <td>
                    <strong>{item.transactionId}</strong>
                    <span>{item.transactionNumber}</span>
                  </td>
                  <td>
                    <StatusBadge label={getDepositStatusLabel(item.status)} tone={getDepositStatusTone(item.status)} />
                  </td>
                  <td>
                    <strong>{item.amount.toFixed(2)} {item.currency}</strong>
                    <span>Channel {item.channel}</span>
                  </td>
                  <td>{new Date(item.requestedAt).toLocaleString()}</td>
                  <td>{item.postedAt ? new Date(item.postedAt).toLocaleString() : 'Pending'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </article>
  )
}
