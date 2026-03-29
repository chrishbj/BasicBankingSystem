import type { DepositResponse, PendingReviewDepositSummaryResponse, PendingReviewSortBy } from '../types'

type PendingReviewPanelProps = {
  sortBy: PendingReviewSortBy
  descending: boolean
  pendingReviewItems: PendingReviewDepositSummaryResponse[]
  depositSearchResult: DepositResponse[]
  onSortByChange: (sortBy: PendingReviewSortBy) => void
  onDescendingChange: (descending: boolean) => void
  onLoadQueue: () => void
  onSearchDeposits: () => void
  onRetry: (transactionId: string) => void
  onResolve: (transactionId: string, resolution: 3 | 4) => void
}

export function PendingReviewPanel({
  sortBy,
  descending,
  pendingReviewItems,
  depositSearchResult,
  onSortByChange,
  onDescendingChange,
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
                <span>{item.status}</span>
                <span>{item.correlationId}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  )
}
