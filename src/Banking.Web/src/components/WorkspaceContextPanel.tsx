import type { AccountResponse, AccountSummaryResponse, CustomerResponse, DepositResponse } from '../types'

type WorkspaceTab = 'customer' | 'account' | 'deposit' | 'review'

type WorkspaceContextPanelProps = {
  customers: CustomerResponse[]
  customer: CustomerResponse | null
  accountList: AccountSummaryResponse[]
  account: AccountResponse | null
  deposit: DepositResponse | null
  busy: boolean
  onSelectCustomer: (customerId: string) => void
  onSelectAccount: (accountId: string) => void
  onNavigate: (tab: WorkspaceTab) => void
}

export function WorkspaceContextPanel({
  customers,
  customer,
  accountList,
  account,
  deposit,
  busy,
  onSelectCustomer,
  onSelectAccount,
  onNavigate,
}: WorkspaceContextPanelProps) {
  return (
    <section className="panel context-panel">
      <div className="panel-head">
        <div>
          <p className="eyebrow">Workspace Context</p>
          <h2>Current Selection</h2>
        </div>
        <div className="actions">
          <button className="ghost-button" type="button" onClick={() => onNavigate('customer')}>Go to customers</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('account')}>Go to accounts</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('deposit')}>Go to deposits</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('review')}>Go to review</button>
        </div>
      </div>

      <div className="context-grid">
        <label className="field-label">
          <span>Selected customer</span>
          <select
            value={customer?.customerId ?? ''}
            onChange={(event) => onSelectCustomer(event.target.value)}
            disabled={busy || customers.length === 0}
          >
            <option value="">Choose a customer</option>
            {customers.map((item) => (
              <option key={item.customerId} value={item.customerId}>
                {item.fullName} | {item.customerNumber}
              </option>
            ))}
          </select>
        </label>

        <label className="field-label">
          <span>Selected account</span>
          <select
            value={account?.accountId ?? ''}
            onChange={(event) => onSelectAccount(event.target.value)}
            disabled={busy || accountList.length === 0}
          >
            <option value="">Choose an account</option>
            {accountList.map((item) => (
              <option key={item.accountId} value={item.accountId}>
                {item.accountType} | {item.accountNumber}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="context-summary-grid">
        <div>
          <dt>Customer</dt>
          <dd>{customer ? `${customer.fullName} (${customer.customerId})` : 'No customer selected'}</dd>
        </div>
        <div>
          <dt>Accounts</dt>
          <dd>{accountList.length > 0 ? `${accountList.length} loaded` : 'No accounts loaded'}</dd>
        </div>
        <div>
          <dt>Current account</dt>
          <dd>{account ? `${account.accountNumber} | ${account.availableBalance.toFixed(2)} ${account.currency}` : 'No account selected'}</dd>
        </div>
        <div>
          <dt>Latest deposit</dt>
          <dd>{deposit ? `${deposit.transactionNumber} | status ${deposit.status}` : 'No deposit selected'}</dd>
        </div>
      </div>
    </section>
  )
}
