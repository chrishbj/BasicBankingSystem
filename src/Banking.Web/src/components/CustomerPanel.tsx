import type { CustomerResponse } from '../types'
import { SectionStatus } from './SectionStatus'
import { StatusBadge } from './StatusBadge'

type CustomerFormState = {
  fullName: string
  identityNumber: string
  mobile: string
  email: string
}

type CustomerPanelProps = {
  customer: CustomerResponse | null
  customers: CustomerResponse[]
  statusText: string
  form: CustomerFormState
  errors: Record<string, string>
  createDisabled: boolean
  busy: boolean
  onFormChange: (next: CustomerFormState) => void
  onCreate: () => void
  onActivate: () => void
  onLoadCustomers: () => void
  onSelectCustomer: (customer: CustomerResponse) => void
}

function getCustomerStatusLabel(status: number) {
  switch (status) {
    case 1:
      return 'Pending'
    case 2:
      return 'Active'
    case 3:
      return 'Frozen'
    default:
      return `Status ${status}`
  }
}

function getCustomerStatusTone(status: number) {
  switch (status) {
    case 2:
      return 'success' as const
    case 3:
      return 'danger' as const
    default:
      return 'warning' as const
  }
}

export function CustomerPanel({
  customer,
  customers,
  statusText,
  form,
  errors,
  createDisabled,
  busy,
  onFormChange,
  onCreate,
  onActivate,
  onLoadCustomers,
  onSelectCustomer,
}: CustomerPanelProps) {
  return (
    <article className="panel wide-panel">
      <div className="panel-head">
        <div>
          <p className="eyebrow">Customer Workspace</p>
          <h2>Customer Management</h2>
          <SectionStatus text={statusText} />
        </div>
        <div className="actions">
          <button onClick={onCreate} disabled={createDisabled}>{busy ? 'Working...' : 'Create customer'}</button>
          <button className="ghost-button" onClick={onActivate} disabled={!customer || busy}>Activate</button>
          <button className="ghost-button" onClick={onLoadCustomers} disabled={busy}>Browse customers</button>
        </div>
      </div>
      <div className="info-card">
        <p className="eyebrow">How To Use</p>
        <p>
          Browse the customer directory, select one customer card, then switch to the Account tab to load that customer's accounts and activity history.
        </p>
      </div>
      <div className="form-grid">
        <label className="field-label">
          <span>Full name</span>
          <input
            value={form.fullName}
            onChange={(event) => onFormChange({ ...form, fullName: event.target.value })}
            placeholder="Example: Alex Chen"
          />
        </label>
        {errors.fullName && <p className="field-error">{errors.fullName}</p>}
        <label className="field-label">
          <span>Identity number</span>
          <input
            value={form.identityNumber}
            onChange={(event) => onFormChange({ ...form, identityNumber: event.target.value })}
            placeholder="Government or bank KYC identity number"
          />
        </label>
        {errors.identityNumber && <p className="field-error">{errors.identityNumber}</p>}
        <label className="field-label">
          <span>Mobile number</span>
          <input
            value={form.mobile}
            onChange={(event) => onFormChange({ ...form, mobile: event.target.value })}
            placeholder="Example: 13800000000"
          />
        </label>
        {errors.mobile && <p className="field-error">{errors.mobile}</p>}
        <label className="field-label">
          <span>Email address</span>
          <input
            value={form.email}
            onChange={(event) => onFormChange({ ...form, email: event.target.value })}
            placeholder="Example: customer@example.com"
          />
        </label>
        {errors.email && <p className="field-error">{errors.email}</p>}
      </div>
      {customer && (
        <dl className="detail-list">
          <div><dt>Customer ID</dt><dd>{customer.customerId}</dd></div>
          <div><dt>Customer Number</dt><dd>{customer.customerNumber}</dd></div>
          <div><dt>Status</dt><dd>{getCustomerStatusLabel(customer.status)}</dd></div>
          <div><dt>Mobile</dt><dd>{customer.mobile}</dd></div>
          <div><dt>Email</dt><dd>{customer.email}</dd></div>
        </dl>
      )}
      {customers.length > 0 && (
        <div className="customer-directory">
          <h3>Existing Customers</h3>
          <div className="customer-directory-grid">
            {customers.map((item) => (
              <button
                key={item.customerId}
                type="button"
                className={item.customerId === customer?.customerId ? 'customer-card customer-card-active' : 'customer-card'}
                onClick={() => onSelectCustomer(item)}
              >
                <div className="customer-card-head">
                  <strong>{item.fullName}</strong>
                  <StatusBadge label={getCustomerStatusLabel(item.status)} tone={getCustomerStatusTone(item.status)} />
                </div>
                <span>{item.customerNumber}</span>
                <span>{item.customerId}</span>
                <span>{item.mobile}</span>
              </button>
            ))}
          </div>
        </div>
      )}
    </article>
  )
}
