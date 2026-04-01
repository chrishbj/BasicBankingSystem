import type { DepositResponse, PendingReviewDepositSummaryResponse, PendingReviewSortBy } from '../types'
import { SectionStatus } from './SectionStatus'
import { StatusBadge, buildDepositBadge, getDepositStatusLabel, getReviewResolutionLabel } from './StatusBadge'
import { formatCurrencyWithCode } from '../utils/currency'

type ReviewSearchState = {
  correlationId: string
  failureCode: string
  status: string
}

type PendingReviewPanelProps = {
  sortBy: PendingReviewSortBy
  descending: boolean
  statusText: string
  reviewSearch: ReviewSearchState
  busy: boolean
  pendingReviewItems: PendingReviewDepositSummaryResponse[]
  depositSearchResult: DepositResponse[]
  selectedCustomerName?: string
  selectedCustomerNumber?: string
  selectedAccountNumber?: string
  onSortByChange: (sortBy: PendingReviewSortBy) => void
  onDescendingChange: (descending: boolean) => void
  onReviewSearchChange: (next: ReviewSearchState) => void
  onLoadQueue: () => void
  onCreateDemo: () => void
  onSearchDeposits: () => void
  onRetry: (transactionId: string) => void
  onResolve: (transactionId: string, resolution: 3 | 4) => void
  onGoToAccount: () => void
}

export function PendingReviewPanel({
  sortBy,
  descending,
  statusText,
  reviewSearch,
  busy,
  pendingReviewItems,
  depositSearchResult,
  selectedCustomerName,
  selectedCustomerNumber,
  selectedAccountNumber,
  onSortByChange,
  onDescendingChange,
  onReviewSearchChange,
  onLoadQueue,
  onCreateDemo,
  onSearchDeposits,
  onRetry,
  onResolve,
  onGoToAccount,
}: PendingReviewPanelProps) {
  return (
    <section className="panel wide-panel">
      <div className="panel-head">
        <div>
          <p className="eyebrow">Operations Search</p>
          <h2>Pending Review Queue</h2>
          <SectionStatus text={statusText} />
        </div>
        <div className="toolbar">
          <label className="field-label compact-field">
            <span>Sort queue by</span>
            <select value={sortBy} onChange={(event) => onSortByChange(event.target.value as PendingReviewSortBy)}>
              <option value="ReviewRequiredAt">Review required</option>
              <option value="LastCompensationAttemptAt">Last compensation attempt</option>
              <option value="RequestedAt">Requested at</option>
            </select>
          </label>
          <label className="toggle">
            <input
              type="checkbox"
              checked={descending}
              onChange={(event) => onDescendingChange(event.target.checked)}
              disabled={busy}
            />
            Descending order
          </label>
          <button onClick={onLoadQueue} disabled={busy}>{busy ? 'Working...' : 'Load queue'}</button>
          <button className="ghost-button" onClick={onCreateDemo} disabled={!selectedCustomerName || !selectedAccountNumber || busy}>
            Create demo review item
          </button>
          <button className="ghost-button" onClick={onSearchDeposits} disabled={busy}>Search matching deposits</button>
        </div>
      </div>

      <div className="info-card">
        <p className="eyebrow">How Pending Review Works</p>
        <p>
          Pending review items appear when a deposit partly succeeds but the compensation step cannot finish automatically.
          In this local environment, you can create a safe demo item for the selected customer and account, then retry or resolve it here.
        </p>
        <div className="actions">
          {selectedCustomerName && <span className="helper-chip">Customer: {selectedCustomerName}</span>}
          {selectedCustomerNumber && <span className="helper-chip">Customer No: {selectedCustomerNumber}</span>}
          {selectedAccountNumber && <span className="helper-chip">Account No: {selectedAccountNumber}</span>}
          {selectedAccountNumber && <button className="ghost-button" type="button" onClick={onGoToAccount}>Open account workspace</button>}
        </div>
      </div>

      <div className="search-grid">
        <label className="field-label">
          <span>Correlation ID</span>
          <input
            value={reviewSearch.correlationId}
            onChange={(event) => onReviewSearchChange({ ...reviewSearch, correlationId: event.target.value })}
            placeholder="Correlation ID"
            disabled={busy}
          />
        </label>
        <label className="field-label">
          <span>Failure code</span>
          <input
            value={reviewSearch.failureCode}
            onChange={(event) => onReviewSearchChange({ ...reviewSearch, failureCode: event.target.value })}
            placeholder="Failure code"
            disabled={busy}
          />
        </label>
        <label className="field-label">
          <span>Deposit status</span>
          <select
            value={reviewSearch.status}
            onChange={(event) => onReviewSearchChange({ ...reviewSearch, status: event.target.value })}
            disabled={busy}
          >
            <option value="">Any status</option>
            <option value="PendingReview">Pending Review</option>
            <option value="Failed">Failed</option>
            <option value="Succeeded">Succeeded</option>
            <option value="Reversed">Reversed</option>
          </select>
        </label>
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
                    <strong>{item.transactionNumber}</strong>
                    <span>{formatCurrencyWithCode(item.amount, item.currency)}</span>
                  </td>
                  <td>
                    <StatusBadge
                      label={buildDepositBadge(item).label}
                      tone={buildDepositBadge(item).tone}
                    />
                    <span>{item.failureCode ?? 'N/A'}</span>
                  </td>
                  <td>{item.compensationRetryCount}</td>
                  <td>{item.reviewRequiredAt ?? item.requestedAt}</td>
                <td className="table-actions">
                  <button className="tiny-button" onClick={() => onRetry(item.transactionId)} disabled={busy}>Retry</button>
                  <button className="tiny-button ghost-button" onClick={() => onResolve(item.transactionId, 3)} disabled={busy}>
                    Mark reversed
                  </button>
                  <button className="tiny-button ghost-button" onClick={() => onResolve(item.transactionId, 4)} disabled={busy}>
                    Mark failed
                  </button>
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
                  <strong>{item.transactionNumber}</strong>
                  <span>{getDepositStatusLabel(item.status)}</span>
                  <span>{getReviewResolutionLabel(item.reviewResolution)}</span>
                </li>
              ))}
          </ul>
        </div>
      )}
    </section>
  )
}
