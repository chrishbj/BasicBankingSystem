import type { DepositResponse } from '../types'
import { SectionStatus } from './SectionStatus'
import { StatusBadge, buildDepositBadge } from './StatusBadge'

type DepositFormState = {
  amount: string
  referenceNumber: string
  note: string
}

type DepositPanelProps = {
  deposit: DepositResponse | null
  form: DepositFormState
  statusText: string
  errors: Record<string, string>
  submitDisabled: boolean
  busy: boolean
  onFormChange: (next: DepositFormState) => void
  onSubmit: () => void
  onRefresh: () => void
}

export function DepositPanel({
  deposit,
  form,
  statusText,
  errors,
  submitDisabled,
  busy,
  onFormChange,
  onSubmit,
  onRefresh,
}: DepositPanelProps) {
  const badge = deposit ? buildDepositBadge(deposit) : null

  return (
    <article className="panel">
      <h2>Deposit</h2>
      <SectionStatus text={statusText} />
      <div className="form-grid">
        <label className="field-label">
          <span>Deposit amount</span>
          <input
            value={form.amount}
            onChange={(event) => onFormChange({ ...form, amount: event.target.value })}
            placeholder="Amount in account currency"
          />
        </label>
        {errors.amount && <p className="field-error">{errors.amount}</p>}
        <label className="field-label">
          <span>Reference number</span>
          <input
            value={form.referenceNumber}
            onChange={(event) => onFormChange({ ...form, referenceNumber: event.target.value })}
            placeholder="Cash slip or teller reference"
          />
        </label>
        {errors.referenceNumber && <p className="field-error">{errors.referenceNumber}</p>}
        <label className="field-label">
          <span>Deposit note</span>
          <textarea
            value={form.note}
            onChange={(event) => onFormChange({ ...form, note: event.target.value })}
            placeholder="Operator note for this deposit"
            rows={3}
          />
        </label>
      </div>
      <div className="actions">
        <button onClick={onSubmit} disabled={submitDisabled}>{busy ? 'Working...' : 'Submit deposit'}</button>
        <button className="ghost-button" onClick={onRefresh} disabled={!deposit || busy}>Refresh transaction</button>
      </div>
      {deposit && (
        <dl className="detail-list">
          <div><dt>Transaction</dt><dd>{deposit.transactionId}</dd></div>
          <div><dt>Status</dt><dd>{badge && <StatusBadge label={badge.label} tone={badge.tone} />}</dd></div>
          <div><dt>Correlation</dt><dd>{deposit.correlationId}</dd></div>
          <div><dt>Failure</dt><dd>{deposit.failureCode ?? 'None'}</dd></div>
        </dl>
      )}
    </article>
  )
}
