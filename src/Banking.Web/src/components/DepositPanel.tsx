import type { DepositResponse } from '../types'
import { SectionStatus } from './SectionStatus'
import { StatusBadge, buildDepositBadge } from './StatusBadge'

type DepositFormState = {
  amount: string
  referenceNumber: string
  note: string
}

type DepositPanelProps = {
  customerName?: string
  customerNumber?: string
  accountNumber?: string
  accountCurrency?: string
  deposit: DepositResponse | null
  form: DepositFormState
  statusText: string
  errors: Record<string, string>
  submitDisabled: boolean
  withdrawDisabled: boolean
  busy: boolean
  onFormChange: (next: DepositFormState) => void
  onSubmit: () => void
  onWithdraw: () => void
  onRefresh: () => void
}

export function DepositPanel({
  customerName,
  customerNumber,
  accountNumber,
  accountCurrency,
  deposit,
  form,
  statusText,
  errors,
  submitDisabled,
  withdrawDisabled,
  busy,
  onFormChange,
  onSubmit,
  onWithdraw,
  onRefresh,
}: DepositPanelProps) {
  const badge = deposit ? buildDepositBadge(deposit) : null

  return (
    <article className="panel">
      <h2>Deposit</h2>
      <SectionStatus text={statusText} />
      <div className="info-card">
        <p className="eyebrow">Deposit Target</p>
        <p>
          {customerName && accountNumber
            ? `This deposit will be submitted for ${customerName}${customerNumber ? ` (${customerNumber})` : ''}, account ${accountNumber}.`
            : 'Select a customer and one of its accounts before submitting a deposit.'}
        </p>
        <div className="actions">
          {customerName && <span className="helper-chip">Customer: {customerName}</span>}
          {customerNumber && <span className="helper-chip">Customer No: {customerNumber}</span>}
          {accountNumber && <span className="helper-chip">Account No: {accountNumber}</span>}
          {accountCurrency && <span className="helper-chip">Currency: {accountCurrency}</span>}
        </div>
      </div>
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
        <p className="field-help">
          Reference number is the business receipt number for this transaction, such as a teller slip, ATM receipt, transfer receipt, or operator ticket number.
        </p>
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
        <button className="ghost-button" onClick={onWithdraw} disabled={withdrawDisabled}>{busy ? 'Working...' : 'Submit withdrawal'}</button>
        <button className="ghost-button" onClick={onRefresh} disabled={!deposit || busy}>Refresh transaction</button>
      </div>
      {deposit && (
        <dl className="detail-list">
          <div><dt>Transaction Number</dt><dd>{deposit.transactionNumber}</dd></div>
          <div><dt>Status</dt><dd>{badge && <StatusBadge label={badge.label} tone={badge.tone} />}</dd></div>
          <div><dt>Correlation</dt><dd>{deposit.correlationId}</dd></div>
          <div><dt>Failure</dt><dd>{deposit.failureCode ?? 'None'}</dd></div>
          <div><dt>Internal Reference</dt><dd className="subtle-code">{deposit.transactionId}</dd></div>
        </dl>
      )}
    </article>
  )
}
