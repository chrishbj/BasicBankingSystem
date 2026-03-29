import './App.css'
import { AccountPanel } from './components/AccountPanel'
import { CustomerPanel } from './components/CustomerPanel'
import { DepositPanel } from './components/DepositPanel'
import { EnvironmentPanel } from './components/EnvironmentPanel'
import { PendingReviewPanel } from './components/PendingReviewPanel'
import { ToastBar } from './components/ToastBar'
import { useOperationsConsole } from './hooks/useOperationsConsole'

function App() {
  const {
    health,
    message,
    toast,
    busyAction,
    depositStatusText,
    reviewStatusText,
    customer,
    account,
    deposit,
    depositSearchResult,
    pendingReviewItems,
    sortBy,
    descending,
    reviewSearch,
    customerForm,
    depositForm,
    customerFormErrors,
    depositFormErrors,
    canCreateCustomer,
    canSubmitDeposit,
    setSortBy,
    setDescending,
    setReviewSearch,
    setCustomerForm,
    setDepositForm,
    loadHealth,
    handleCreateCustomer,
    handleActivateCustomer,
    handleOpenAccount,
    handleRefreshAccount,
    handleSubmitDeposit,
    handleRefreshDeposit,
    handleSearchDeposits,
    handleLoadPendingReview,
    handleRetryPendingReview,
    handleResolvePendingReview,
  } = useOperationsConsole()

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

      <section className="grid">
        <EnvironmentPanel health={health} />
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
        <AccountPanel
          account={account}
          openDisabled={!customer || !!busyAction}
          busy={!!busyAction}
          onOpen={() => void handleOpenAccount()}
          onRefresh={() => void handleRefreshAccount()}
        />
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
      </section>

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
    </main>
  )
}

export default App
