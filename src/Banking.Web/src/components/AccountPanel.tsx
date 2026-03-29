import type { AccountResponse } from '../types'

type AccountPanelProps = {
  account: AccountResponse | null
  openDisabled: boolean
  busy: boolean
  onOpen: () => void
  onRefresh: () => void
}

export function AccountPanel({ account, openDisabled, busy, onOpen, onRefresh }: AccountPanelProps) {
  return (
    <article className="panel">
      <h2>Account</h2>
      <div className="actions">
        <button onClick={onOpen} disabled={openDisabled}>{busy ? 'Working...' : 'Open checking account'}</button>
        <button className="ghost-button" onClick={onRefresh} disabled={!account || busy}>Refresh</button>
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
