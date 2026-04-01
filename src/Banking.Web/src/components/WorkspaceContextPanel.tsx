import type { AccountResponse, AccountSummaryResponse, CustomerResponse, DepositResponse } from '../types'
import { formatCurrency } from '../utils/currency'

type WorkspaceTab = 'overview' | 'customer' | 'account' | 'deposit' | 'withdraw' | 'review'

type WorkspaceContextPanelProps = {
  customers: CustomerResponse[]
  customer: CustomerResponse | null
  accountList: AccountSummaryResponse[]
  account: AccountResponse | null
  deposit: DepositResponse | null
  busy: boolean
  onSelectCustomer: (customerNumber: string) => void
  onSelectAccount: (accountNumber: string) => void
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
          <button className="ghost-button" type="button" onClick={() => onNavigate('overview')}>Go to overview</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('customer')}>Go to customers</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('account')}>Go to accounts</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('deposit')}>Go to deposits</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('withdraw')}>Go to withdrawals</button>
          <button className="ghost-button" type="button" onClick={() => onNavigate('review')}>Go to review</button>
        </div>
      </div>

      <div className="context-grid">
        <label className="field-label">
          <span>Selected customer</span>
          <select
            value={customer?.customerNumber ?? ''}
            onChange={(event) => onSelectCustomer(event.target.value)}
            disabled={busy || customers.length === 0}
          >
            <option value="">Choose a customer</option>
            {customers.map((item) => (
              <option key={item.customerNumber} value={item.customerNumber}>
                {item.fullName} | {item.customerNumber}
              </option>
            ))}
          </select>
        </label>

        <label className="field-label">
          <span>Selected account</span>
          <select
            value={account?.accountNumber ?? ''}
            onChange={(event) => onSelectAccount(event.target.value)}
            disabled={busy || accountList.length === 0}
          >
            <option value="">Choose an account</option>
            {accountList.map((item) => (
              <option key={item.accountNumber} value={item.accountNumber}>
                {item.accountType} | {item.accountNumber}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="context-summary-grid">
        <div>
          <dt>Customer</dt>
          <dd>{customer ? customer.fullName : 'No customer selected'}</dd>
        </div>
        <div>
          <dt>Customer number</dt>
          <dd>{customer?.customerNumber ?? 'No customer selected'}</dd>
        </div>
        <div>
          <dt>Identity number</dt>
          <dd>{customer?.identityNumberMasked ?? 'No customer selected'}</dd>
        </div>
        <div>
          <dt>Portal sign-in last 4 digits</dt>
          <dd>{customer?.portalIdentityLast4 ?? 'No customer selected'}</dd>
        </div>
        <div>
          <dt>Accounts</dt>
          <dd>{accountList.length > 0 ? `${accountList.length} loaded` : 'No accounts loaded'}</dd>
        </div>
        <div>
          <dt>Current account</dt>
          <dd>{account ? `${account.accountNumber} | ${formatCurrency(account.availableBalance, account.currency)}` : 'No account selected'}</dd>
        </div>
        <div>
          <dt>Latest deposit</dt>
          <dd>{deposit ? `${deposit.transactionNumber} | status ${deposit.status}` : 'No deposit selected'}</dd>
        </div>
      </div>
    </section>
  )
}
