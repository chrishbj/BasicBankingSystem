import './App.css'
import { AccountPanel } from './components/AccountPanel'
import { CustomerPanel } from './components/CustomerPanel'
import { DepositPanel } from './components/DepositPanel'
import { EnvironmentPanel } from './components/EnvironmentPanel'
import { PendingReviewPanel } from './components/PendingReviewPanel'
import { ToastBar } from './components/ToastBar'
import { useOperationsConsole } from './hooks/useOperationsConsole'
import { useState } from 'react'

type WorkspaceTab = 'customer' | 'account' | 'deposit' | 'review'

function App() {
  const [activeTab, setActiveTab] = useState<WorkspaceTab>('customer')
  const {
    health,
    message,
    toast,
    busyAction,
    depositStatusText,
    accountHistoryStatusText,
    reviewStatusText,
    customer,
    account,
    accountList,
    deposit,
    accountHistory,
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
    handleCreateCustomer,
    handleActivateCustomer,
    handleOpenAccount,
    handleRefreshAccount,
    handleLookupAccount,
    handleLoadCustomerAccounts,
    handleSelectAccount,
    handleLoadAccountHistory,
    handleSubmitDeposit,
    handleRefreshDeposit,
    handleSearchDeposits,
    handleLoadPendingReview,
    handleRetryPendingReview,
    handleResolvePendingReview,
    setSelectedAccountHistoryItem,
  } = useOperationsConsole()

  const tabs: Array<{ id: WorkspaceTab; label: string; hint: string }> = [
    { id: 'customer', label: 'Customer', hint: 'Onboard and activate a customer' },
    { id: 'account', label: 'Account', hint: 'Open and verify accounts' },
    { id: 'deposit', label: 'Deposit', hint: 'Submit and monitor deposits' },
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
            deposit submission, and pending-review recovery.
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
          {activeTab === 'customer' && (
            <CustomerPanel
              customer={customer}
              form={customerForm}
              errors={customerFormErrors}
              createDisabled={!canCreateCustomer}
              busy={!!busyAction}
              onFormChange={setCustomerForm}
              onCreate={() => void handleCreateCustomer()}
              onActivate={() => void handleActivateCustomer()}
            />
          )}

          {activeTab === 'account' && (
            <AccountPanel
              customerId={customer?.customerId}
              account={account}
              accountList={accountList}
              lookupAccountId={accountQuery.accountId}
              historyStatusText={accountHistoryStatusText}
              history={accountHistory}
              selectedHistoryItem={selectedAccountHistoryItem}
              historyFilters={accountHistoryFilters}
              openDisabled={!customer || !!busyAction}
              lookupDisabled={!accountQuery.accountId.trim() || !!busyAction}
              loadHistoryDisabled={!(accountQuery.accountId.trim() || account?.accountId) || !!busyAction}
              busy={!!busyAction}
              onLookupAccountIdChange={(accountId) => setAccountQuery({ accountId })}
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
              deposit={deposit}
              form={depositForm}
              statusText={depositStatusText}
              errors={depositFormErrors}
              submitDisabled={!canSubmitDeposit}
              busy={!!busyAction}
              onFormChange={setDepositForm}
              onSubmit={() => void handleSubmitDeposit()}
              onRefresh={() => void handleRefreshDeposit()}
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
              onSortByChange={setSortBy}
              onDescendingChange={setDescending}
              onReviewSearchChange={setReviewSearch}
              onLoadQueue={() => void handleLoadPendingReview()}
              onSearchDeposits={() => void handleSearchDeposits()}
              onRetry={(transactionId) => void handleRetryPendingReview(transactionId)}
              onResolve={(transactionId, resolution) => void handleResolvePendingReview(transactionId, resolution)}
            />
          )}
        </section>
      </section>
    </main>
  )
}

export default App
