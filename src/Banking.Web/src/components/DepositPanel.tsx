import type { DepositResponse } from '../types'

type DepositFormState = {
  amount: string
  referenceNumber: string
  note: string
}

type DepositPanelProps = {
  deposit: DepositResponse | null
  form: DepositFormState
  onFormChange: (next: DepositFormState) => void
  onSubmit: () => void
  onRefresh: () => void
}

export function DepositPanel({
  deposit,
  form,
  onFormChange,
  onSubmit,
  onRefresh,
}: DepositPanelProps) {
  return (
    <article className="panel">
      <h2>Deposit</h2>
      <div className="form-grid">
        <input
          value={form.amount}
          onChange={(event) => onFormChange({ ...form, amount: event.target.value })}
          placeholder="Amount"
        />
        <input
          value={form.referenceNumber}
          onChange={(event) => onFormChange({ ...form, referenceNumber: event.target.value })}
          placeholder="Reference number"
        />
        <textarea
          value={form.note}
          onChange={(event) => onFormChange({ ...form, note: event.target.value })}
          placeholder="Deposit note"
          rows={3}
        />
      </div>
      <div className="actions">
        <button onClick={onSubmit}>Submit deposit</button>
        <button className="ghost-button" onClick={onRefresh}>Refresh transaction</button>
      </div>
      {deposit && (
        <dl className="detail-list">
          <div><dt>Transaction</dt><dd>{deposit.transactionId}</dd></div>
          <div><dt>Status</dt><dd>{deposit.status}</dd></div>
          <div><dt>Correlation</dt><dd>{deposit.correlationId}</dd></div>
          <div><dt>Failure</dt><dd>{deposit.failureCode ?? 'None'}</dd></div>
        </dl>
      )}
    </article>
  )
}
