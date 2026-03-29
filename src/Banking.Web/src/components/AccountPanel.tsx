import type { AccountResponse } from '../types'

type AccountPanelProps = {
  account: AccountResponse | null
  onOpen: () => void
  onRefresh: () => void
}

export function AccountPanel({ account, onOpen, onRefresh }: AccountPanelProps) {
  return (
    <article className="panel">
      <h2>Account</h2>
      <div className="actions">
        <button onClick={onOpen}>Open checking account</button>
        <button className="ghost-button" onClick={onRefresh}>Refresh</button>
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
  )
}
