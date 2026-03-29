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
  onFormChange: (next: CustomerFormState) => void
  onCreate: () => void
  onActivate: () => void
}

export function CustomerPanel({
  customer,
  form,
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
        <input
          value={form.identityNumber}
          onChange={(event) => onFormChange({ ...form, identityNumber: event.target.value })}
          placeholder="Identity number"
        />
        <input
          value={form.mobile}
          onChange={(event) => onFormChange({ ...form, mobile: event.target.value })}
          placeholder="Mobile"
        />
        <input
          value={form.email}
          onChange={(event) => onFormChange({ ...form, email: event.target.value })}
          placeholder="Email"
        />
      </div>
      <div className="actions">
        <button onClick={onCreate}>Create customer</button>
        <button className="ghost-button" onClick={onActivate}>Activate</button>
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
