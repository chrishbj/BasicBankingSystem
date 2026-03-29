import type { CustomerResponse } from '../types'

type CustomerFormState = {
  fullName: string
  identityNumber: string
  mobile: string
  email: string
}

type CustomerPanelProps = {
  customer: CustomerResponse | null
  form: CustomerFormState
  errors: Record<string, string>
  createDisabled: boolean
  busy: boolean
  onFormChange: (next: CustomerFormState) => void
  onCreate: () => void
  onActivate: () => void
}

export function CustomerPanel({
  customer,
  form,
  errors,
  createDisabled,
  busy,
  onFormChange,
  onCreate,
  onActivate,
}: CustomerPanelProps) {
  return (
    <article className="panel">
      <h2>Customer</h2>
      <div className="form-grid">
        <input
          value={form.fullName}
          onChange={(event) => onFormChange({ ...form, fullName: event.target.value })}
          placeholder="Full name"
        />
        {errors.fullName && <p className="field-error">{errors.fullName}</p>}
        <input
          value={form.identityNumber}
          onChange={(event) => onFormChange({ ...form, identityNumber: event.target.value })}
          placeholder="Identity number"
        />
        {errors.identityNumber && <p className="field-error">{errors.identityNumber}</p>}
        <input
          value={form.mobile}
          onChange={(event) => onFormChange({ ...form, mobile: event.target.value })}
          placeholder="Mobile"
        />
        {errors.mobile && <p className="field-error">{errors.mobile}</p>}
        <input
          value={form.email}
          onChange={(event) => onFormChange({ ...form, email: event.target.value })}
          placeholder="Email"
        />
        {errors.email && <p className="field-error">{errors.email}</p>}
      </div>
      <div className="actions">
        <button onClick={onCreate} disabled={createDisabled}>{busy ? 'Working...' : 'Create customer'}</button>
        <button className="ghost-button" onClick={onActivate} disabled={!customer || busy}>Activate</button>
      </div>
      {customer && (
        <dl className="detail-list">
          <div><dt>ID</dt><dd>{customer.customerId}</dd></div>
          <div><dt>Number</dt><dd>{customer.customerNumber}</dd></div>
          <div><dt>Status</dt><dd>{customer.status}</dd></div>
        </dl>
      )}
    </article>
  )
}
