import type { DepositResponse, PendingReviewDepositSummaryResponse, PendingReviewSortBy } from '../types'
import { SectionStatus } from './SectionStatus'
import { StatusBadge, buildDepositBadge, getDepositStatusLabel, getReviewResolutionLabel } from './StatusBadge'

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
  pendingReviewItems: PendingReviewDepositSummaryResponse[]
  depositSearchResult: DepositResponse[]
  onSortByChange: (sortBy: PendingReviewSortBy) => void
  onDescendingChange: (descending: boolean) => void
  onReviewSearchChange: (next: ReviewSearchState) => void
  onLoadQueue: () => void
  onSearchDeposits: () => void
  onRetry: (transactionId: string) => void
  onResolve: (transactionId: string, resolution: 3 | 4) => void
}

export function PendingReviewPanel({
  sortBy,
  descending,
  statusText,
  reviewSearch,
  pendingReviewItems,
  depositSearchResult,
  onSortByChange,
  onDescendingChange,
  onReviewSearchChange,
  onLoadQueue,
  onSearchDeposits,
  onRetry,
  onResolve,
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
          <select value={sortBy} onChange={(event) => onSortByChange(event.target.value as PendingReviewSortBy)}>
            <option value="ReviewRequiredAt">Review required</option>
            <option value="LastCompensationAttemptAt">Last compensation attempt</option>
            <option value="RequestedAt">Requested at</option>
          </select>
          <label className="toggle">
            <input
              type="checkbox"
              checked={descending}
              onChange={(event) => onDescendingChange(event.target.checked)}
            />
            Desc
          </label>
          <button onClick={onLoadQueue}>Load queue</button>
          <button className="ghost-button" onClick={onSearchDeposits}>Search matching deposits</button>
        </div>
      </div>

      <div className="search-grid">
        <input
          value={reviewSearch.correlationId}
          onChange={(event) => onReviewSearchChange({ ...reviewSearch, correlationId: event.target.value })}
          placeholder="Correlation ID"
        />
        <input
          value={reviewSearch.failureCode}
          onChange={(event) => onReviewSearchChange({ ...reviewSearch, failureCode: event.target.value })}
          placeholder="Failure code"
        />
        <select
          value={reviewSearch.status}
          onChange={(event) => onReviewSearchChange({ ...reviewSearch, status: event.target.value })}
        >
          <option value="">Any status</option>
          <option value="PendingReview">Pending Review</option>
          <option value="Failed">Failed</option>
          <option value="Succeeded">Succeeded</option>
          <option value="Reversed">Reversed</option>
        </select>
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
                  <button className="tiny-button" onClick={() => onRetry(item.transactionId)}>Retry</button>
                  <button className="tiny-button ghost-button" onClick={() => onResolve(item.transactionId, 3)}>
                    Mark reversed
                  </button>
                  <button className="tiny-button ghost-button" onClick={() => onResolve(item.transactionId, 4)}>
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
                  <strong>{item.transactionId}</strong>
                  <span>{getDepositStatusLabel(item.status)}</span>
                  <span>{getReviewResolutionLabel(item.reviewResolution)}</span>
                  <span>{item.correlationId}</span>
                </li>
              ))}
          </ul>
        </div>
      )}
    </section>
  )
}
