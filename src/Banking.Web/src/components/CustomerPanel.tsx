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
  selectedCustomerId?: string
  selectedCustomerStatus?: number
  customers: CustomerResponse[]
  statusText: string
  form: CustomerFormState
  errors: Record<string, string>
  createDisabled: boolean
  activateDisabled: boolean
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
  selectedCustomerId,
  selectedCustomerStatus,
  customers,
  statusText,
  form,
  errors,
  createDisabled,
  activateDisabled,
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
          <button className="ghost-button" onClick={onActivate} disabled={activateDisabled}>
            {busy ? 'Working...' : 'Activate selected customer'}
          </button>
          <button className="ghost-button" onClick={onLoadCustomers} disabled={busy}>Browse customers</button>
        </div>
      </div>
      <div className="info-card">
        <p className="eyebrow">How To Use</p>
        <p>
          Browse the customer directory and select one customer card from the list below. New customers start in the Pending state, so activate the selected customer before opening an account.
        </p>
      </div>
      {selectedCustomerId && selectedCustomerStatus === 1 && (
        <div className="info-card">
          <p className="eyebrow">Activation Required</p>
          <p>The selected customer is still Pending. Use <strong>Activate selected customer</strong> before opening the first account.</p>
        </div>
      )}
      <div className="info-card">
        <p className="eyebrow">New Customer</p>
        <p>Create a new customer profile here. After creation, the new customer is automatically selected in the workspace and can then be activated from this page.</p>
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
      {customers.length > 0 && (
        <div className="customer-directory">
          <h3>Existing Customers</h3>
          <div className="customer-directory-grid">
            {customers.map((item) => (
              <button
                key={item.customerId}
                type="button"
                className={item.customerId === selectedCustomerId ? 'customer-card customer-card-active' : 'customer-card'}
                onClick={() => onSelectCustomer(item)}
              >
                <div className="customer-card-head">
                  <strong>{item.fullName}</strong>
                  <StatusBadge label={getCustomerStatusLabel(item.status)} tone={getCustomerStatusTone(item.status)} />
                </div>
                <div className="card-metadata">
                  <span className="card-label">Customer Number</span>
                  <span className="card-value">{item.customerNumber}</span>
                </div>
                <div className="card-metadata">
                  <span className="card-label">Identity Number</span>
                  <span className="card-value">{item.identityNumberMasked}</span>
                </div>
                <div className="card-metadata">
                  <span className="card-label">Portal Sign-In Last 4 Digits</span>
                  <span className="card-value">{item.portalIdentityLast4}</span>
                </div>
                <div className="card-metadata">
                  <span className="card-label">Mobile</span>
                  <span className="card-value">{item.mobile}</span>
                </div>
                <span className="subtle-code">{item.customerId}</span>
              </button>
            ))}
          </div>
        </div>
      )}
    </article>
  )
}
