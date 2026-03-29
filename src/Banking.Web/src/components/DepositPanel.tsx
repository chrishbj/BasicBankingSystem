import type { DepositResponse } from '../types'
import { SectionStatus } from './SectionStatus'
import { StatusBadge, buildDepositBadge } from './StatusBadge'
import { formatCurrency } from '../utils/currency'

type DepositFormState = {
  amount: string
  referenceNumber: string
  note: string
}

type DepositPanelProps = {
  mode: 'deposit' | 'withdraw'
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
  mode,
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
  const isWithdraw = mode === 'withdraw'
  const title = isWithdraw ? 'Withdraw' : 'Deposit'
  const targetLabel = isWithdraw ? 'Withdrawal Target' : 'Deposit Target'
  const amountLabel = isWithdraw ? 'Withdrawal amount' : 'Deposit amount'
  const noteLabel = isWithdraw ? 'Withdrawal note' : 'Deposit note'
  const notePlaceholder = isWithdraw ? 'Operator note for this withdrawal' : 'Operator note for this deposit'
  const helperText = isWithdraw
    ? 'Reference number is the business receipt number for this withdrawal, such as a branch slip, ATM receipt, or operator ticket number.'
    : 'Reference number is the business receipt number for this transaction, such as a teller slip, ATM receipt, transfer receipt, or operator ticket number.'

  return (
    <article className="panel">
      <h2>{title}</h2>
      <SectionStatus text={statusText} />
      <div className="info-card">
        <p className="eyebrow">{targetLabel}</p>
        <p>
          {customerName && accountNumber
            ? `This ${mode} will be submitted for ${customerName}${customerNumber ? ` (${customerNumber})` : ''}, account ${accountNumber}.`
            : `Select a customer and one of its accounts before submitting a ${mode}.`}
        </p>
        <div className="actions">
          {customerName && <span className="helper-chip">Customer: {customerName}</span>}
          {customerNumber && <span className="helper-chip">Customer No: {customerNumber}</span>}
          {accountNumber && <span className="helper-chip">Account No: {accountNumber}</span>}
          {accountCurrency && <span className="helper-chip">Currency: {accountCurrency} ($)</span>}
        </div>
      </div>
      <div className="form-grid">
        <label className="field-label">
          <span>{amountLabel}</span>
          <input
            value={form.amount}
            onChange={(event) => onFormChange({ ...form, amount: event.target.value })}
            placeholder="Amount in USD"
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
        <p className="field-help">{helperText}</p>
        <label className="field-label">
          <span>{noteLabel}</span>
          <textarea
            value={form.note}
            onChange={(event) => onFormChange({ ...form, note: event.target.value })}
            placeholder={notePlaceholder}
            rows={3}
          />
        </label>
      </div>
      <div className="actions">
        {!isWithdraw && <button onClick={onSubmit} disabled={submitDisabled}>{busy ? 'Working...' : 'Submit deposit'}</button>}
        {isWithdraw && <button onClick={onWithdraw} disabled={withdrawDisabled}>{busy ? 'Working...' : 'Submit withdrawal'}</button>}
        {!isWithdraw && <button className="ghost-button" onClick={onRefresh} disabled={!deposit || busy}>Refresh transaction</button>}
      </div>
      {deposit && (
        <dl className="detail-list">
          <div><dt>Transaction Number</dt><dd>{deposit.transactionNumber}</dd></div>
          <div><dt>Status</dt><dd>{badge && <StatusBadge label={badge.label} tone={badge.tone} />}</dd></div>
          <div><dt>Amount</dt><dd>{formatCurrency(deposit.amount, deposit.currency)}</dd></div>
          <div><dt>Correlation</dt><dd>{deposit.correlationId}</dd></div>
          <div><dt>Failure</dt><dd>{deposit.failureCode ?? 'None'}</dd></div>
          <div><dt>Internal Reference</dt><dd className="subtle-code">{deposit.transactionId}</dd></div>
        </dl>
      )}
    </article>
  )
}
