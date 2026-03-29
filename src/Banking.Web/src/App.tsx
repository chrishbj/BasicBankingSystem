import './App.css'
import { AccountPanel } from './components/AccountPanel'
import { CustomerPanel } from './components/CustomerPanel'
import { DepositPanel } from './components/DepositPanel'
import { EnvironmentPanel } from './components/EnvironmentPanel'
import { OverviewPanel } from './components/OverviewPanel'
import { PendingReviewPanel } from './components/PendingReviewPanel'
import { ToastBar } from './components/ToastBar'
import { WorkspaceContextPanel } from './components/WorkspaceContextPanel'
import { useOperationsConsole } from './hooks/useOperationsConsole'
import { useState } from 'react'

type WorkspaceTab = 'overview' | 'customer' | 'account' | 'deposit' | 'withdraw' | 'review'

function App() {
  const [activeTab, setActiveTab] = useState<WorkspaceTab>('overview')
  const {
    health,
    message,
    toast,
    busyAction,
    customerStatusText,
    depositStatusText,
    accountHistoryStatusText,
    reviewStatusText,
    customer,
    customers,
    account,
    accountList,
    deposit,
    accountHistory,
    customerActivitySnapshot,
    selectedAccountHistoryItem,
    depositSearchResult,
    pendingReviewItems,
    sortBy,
    descending,
    reviewSearch,
    accountQuery,
    accountHistoryFilters,
    customerForm,
    depositForm,
    customerFormErrors,
    depositFormErrors,
    canCreateCustomer,
    canSubmitDeposit,
    setSortBy,
    setDescending,
    setReviewSearch,
    setAccountQuery,
    setAccountHistoryFilters,
    setCustomerForm,
    setDepositForm,
    loadHealth,
    handleLoadCustomers,
    handleSelectCustomer,
    handleCreateCustomer,
    handleOpenAccount,
    handleRefreshAccount,
    handleLookupAccount,
    handleLoadCustomerAccounts,
    handleSelectAccount,
    handleLoadAccountHistory,
    handleSubmitDeposit,
    handleSubmitWithdrawal,
    handleRefreshDeposit,
    handleSearchDeposits,
    handleLoadPendingReview,
    handleCreatePendingReviewDemo,
    handleRetryPendingReview,
    handleResolvePendingReview,
    setSelectedAccountHistoryItem,
  } = useOperationsConsole()

  const tabs: Array<{ id: WorkspaceTab; label: string; hint: string }> = [
    { id: 'overview', label: 'Overview', hint: 'See customer, account, and review summaries' },
    { id: 'customer', label: 'Customer', hint: 'Onboard and activate a customer' },
    { id: 'account', label: 'Account', hint: 'Open and verify accounts' },
    { id: 'deposit', label: 'Deposit', hint: 'Submit and monitor deposits' },
    { id: 'withdraw', label: 'Withdraw', hint: 'Submit cash-out transactions' },
    { id: 'review', label: 'Review', hint: 'Resolve pending review items' },
  ]

  return (
    <main className="app-shell">
      <ToastBar text={toast} />
      <section className="hero-panel">
        <div>
          <p className="eyebrow">Basic Banking System</p>
          <h1>Operations Console</h1>
          <p className="intro">
            A frontend-first operator workspace for customer onboarding, account opening,
            deposit submission, withdrawals, and pending-review recovery.
          </p>
        </div>
        <div className="status-strip">
          <button className="ghost-button" onClick={() => void loadHealth()}>
            Refresh health
          </button>
          <p>{message}</p>
        </div>
      </section>

      <section className="workspace-shell">
        <aside className="workspace-sidebar">
          <EnvironmentPanel health={health} />
          <nav className="workspace-nav" aria-label="Operations workspace tabs">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                className={tab.id === activeTab ? 'workspace-tab workspace-tab-active' : 'workspace-tab'}
                onClick={() => setActiveTab(tab.id)}
                type="button"
              >
                <strong>{tab.label}</strong>
                <span>{tab.hint}</span>
              </button>
            ))}
          </nav>
        </aside>

        <section className="workspace-content">
          <WorkspaceContextPanel
            customers={customers}
            customer={customer}
            accountList={accountList}
            account={account}
            deposit={deposit}
            busy={!!busyAction}
            onSelectCustomer={(customerId) => {
              const selected = customers.find((item) => item.customerId === customerId)
              if (selected) {
                void handleSelectCustomer(selected)
              }
            }}
            onSelectAccount={(accountId) => void handleSelectAccount(accountId)}
            onNavigate={setActiveTab}
          />

          {activeTab === 'overview' && (
            <OverviewPanel
              customer={customer}
              customers={customers}
              account={account}
              accountList={accountList}
              accountHistory={accountHistory}
              customerActivitySnapshot={customerActivitySnapshot}
              pendingReviewItems={pendingReviewItems}
              onNavigate={setActiveTab}
            />
          )}

          {activeTab === 'customer' && (
            <CustomerPanel
              selectedCustomerId={customer?.customerId}
              customers={customers}
              statusText={customerStatusText}
              form={customerForm}
              errors={customerFormErrors}
              createDisabled={!canCreateCustomer}
              busy={!!busyAction}
              onFormChange={setCustomerForm}
              onCreate={() => void handleCreateCustomer()}
              onLoadCustomers={() => void handleLoadCustomers()}
              onSelectCustomer={(selectedCustomer) => void handleSelectCustomer(selectedCustomer)}
            />
          )}

          {activeTab === 'account' && (
            <AccountPanel
              customerName={customer?.fullName}
              customerNumber={customer?.customerNumber}
              account={account}
              accountList={accountList}
              lookupAccountNumber={accountQuery.accountNumber}
              historyStatusText={accountHistoryStatusText}
              history={accountHistory}
              selectedHistoryItem={selectedAccountHistoryItem}
              historyFilters={accountHistoryFilters}
              openDisabled={!customer || !!busyAction}
              lookupDisabled={!accountQuery.accountNumber.trim() || !!busyAction}
              loadHistoryDisabled={!account?.accountId || !!busyAction}
              busy={!!busyAction}
              onLookupAccountNumberChange={(accountNumber) => setAccountQuery({ accountNumber })}
              onHistoryFiltersChange={setAccountHistoryFilters}
              onOpen={() => void handleOpenAccount()}
              onRefresh={() => void handleRefreshAccount()}
              onLookup={() => void handleLookupAccount()}
              onLoadCustomerAccounts={() => void handleLoadCustomerAccounts()}
              onSelectAccount={(accountId) => void handleSelectAccount(accountId)}
              onLoadHistory={() => void handleLoadAccountHistory()}
              onSelectHistoryItem={setSelectedAccountHistoryItem}
            />
          )}

          {activeTab === 'deposit' && (
            <DepositPanel
              mode="deposit"
              customerName={customer?.fullName}
              customerNumber={customer?.customerNumber}
              accountNumber={account?.accountNumber}
              accountCurrency={account?.currency}
              deposit={deposit}
              form={depositForm}
              statusText={depositStatusText}
              errors={depositFormErrors}
              submitDisabled={!canSubmitDeposit}
              withdrawDisabled={!canSubmitDeposit}
              busy={!!busyAction}
              onFormChange={setDepositForm}
              onSubmit={() => void handleSubmitDeposit()}
              onWithdraw={() => void handleSubmitWithdrawal()}
              onRefresh={() => void handleRefreshDeposit()}
              onGoToAccount={() => setActiveTab('account')}
            />
          )}

          {activeTab === 'withdraw' && (
            <DepositPanel
              mode="withdraw"
              customerName={customer?.fullName}
              customerNumber={customer?.customerNumber}
              accountNumber={account?.accountNumber}
              accountCurrency={account?.currency}
              deposit={deposit}
              form={depositForm}
              statusText={depositStatusText}
              errors={depositFormErrors}
              submitDisabled={!canSubmitDeposit}
              withdrawDisabled={!canSubmitDeposit}
              busy={!!busyAction}
              onFormChange={setDepositForm}
              onSubmit={() => void handleSubmitDeposit()}
              onWithdraw={() => void handleSubmitWithdrawal()}
              onRefresh={() => void handleRefreshDeposit()}
              onGoToAccount={() => setActiveTab('account')}
            />
          )}

          {activeTab === 'review' && (
            <PendingReviewPanel
              sortBy={sortBy}
              descending={descending}
              statusText={reviewStatusText}
              reviewSearch={reviewSearch}
              busy={!!busyAction}
              pendingReviewItems={pendingReviewItems}
              depositSearchResult={depositSearchResult}
              selectedCustomerName={customer?.fullName}
              selectedCustomerNumber={customer?.customerNumber}
              selectedAccountNumber={account?.accountNumber}
              selectedAccountId={account?.accountId}
              onSortByChange={setSortBy}
              onDescendingChange={setDescending}
              onReviewSearchChange={setReviewSearch}
              onLoadQueue={() => void handleLoadPendingReview()}
              onCreateDemo={() => void handleCreatePendingReviewDemo()}
              onSearchDeposits={() => void handleSearchDeposits()}
              onRetry={(transactionId) => void handleRetryPendingReview(transactionId)}
              onResolve={(transactionId, resolution) => void handleResolvePendingReview(transactionId, resolution)}
              onGoToAccount={() => setActiveTab('account')}
            />
          )}
        </section>
      </section>
    </main>
  )
}

export default App
