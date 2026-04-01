import { startTransition, useEffect, useState } from 'react'
import './App.css'
import {
  getCorrelationDiagnostics,
  getDepositOutboxMessages,
  getDepositRuntimeStatus,
  getDepositWorkflowDetail,
  getDepositWorkflowSummary,
  getPendingReviewItems,
  getPlatformOperationsAudit,
  getPlatformOverview,
  getPlatformServices,
  requeueOutboxMessage,
  resolveDepositReview,
  retryDepositCompensation,
} from './api'
import type {
  AuditTraceItem,
  CorrelationDiagnostics,
  DepositOutboxMessageItem,
  DepositRuntimeStatus,
  DepositReviewResolutionOption,
  DepositPendingReviewItem,
  DepositWorkflowDetail,
  DepositWorkflowSummary,
  PlatformMaintenanceAction,
  PlatformOverview,
  PlatformServiceStatus,
} from './types'

type PlatformTab = 'overview' | 'services' | 'workflows' | 'diagnostics' | 'maintenance' | 'audit'

function formatTimestamp(value: string | null) {
  if (!value) {
    return 'n/a'
  }

  return new Date(value).toLocaleString()
}

function buildStatusTone(value: string) {
  return /healthy|succeeded|active/i.test(value) ? 'pill pill-healthy' : 'pill pill-warning'
}

function App() {
  const [activeTab, setActiveTab] = useState<PlatformTab>('overview')
  const [overview, setOverview] = useState<PlatformOverview | null>(null)
  const [services, setServices] = useState<PlatformServiceStatus[]>([])
  const [workflowSummary, setWorkflowSummary] = useState<DepositWorkflowSummary | null>(null)
  const [pendingReviewItems, setPendingReviewItems] = useState<DepositPendingReviewItem[]>([])
  const [outboxItems, setOutboxItems] = useState<DepositOutboxMessageItem[]>([])
  const [runtimeStatus, setRuntimeStatus] = useState<DepositRuntimeStatus | null>(null)
  const [selectedWorkflow, setSelectedWorkflow] = useState<DepositWorkflowDetail | null>(null)
  const [selectedTransactionId, setSelectedTransactionId] = useState('dep-platform-001')
  const [correlationQuery, setCorrelationQuery] = useState('corr-platform-001')
  const [diagnostics, setDiagnostics] = useState<CorrelationDiagnostics | null>(null)
  const [platformAudit, setPlatformAudit] = useState<AuditTraceItem[]>([])
  const [lastMaintenanceAction, setLastMaintenanceAction] = useState<PlatformMaintenanceAction | null>(null)
  const [maintenanceReason, setMaintenanceReason] = useState('Platform maintenance review action')
  const [statusText, setStatusText] = useState('Loading platform overview...')
  const [busy, setBusy] = useState(false)

  async function loadOverview() {
    const [overviewResponse, servicesResponse, workflowResponse, pendingReviewResponse, outboxResponse, runtimeResponse] = await Promise.all([
      getPlatformOverview(),
      getPlatformServices(),
      getDepositWorkflowSummary(),
      getPendingReviewItems(),
      getDepositOutboxMessages(),
      getDepositRuntimeStatus(),
    ])

    startTransition(() => {
      setOverview(overviewResponse)
      setServices(servicesResponse)
      setWorkflowSummary(workflowResponse)
      setPendingReviewItems(pendingReviewResponse)
      setOutboxItems(outboxResponse)
      setRuntimeStatus(runtimeResponse)
    })
  }

  async function loadPlatformAudit() {
    const auditResponse = await getPlatformOperationsAudit()
    startTransition(() => {
      setPlatformAudit(auditResponse)
    })
  }

  async function handleRefresh() {
    setBusy(true)
    setStatusText('Refreshing platform telemetry...')
    try {
      await loadOverview()
      await loadPlatformAudit()
      setStatusText('Platform telemetry refreshed.')
    } catch (error) {
      setStatusText(`Could not refresh platform telemetry: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function handleLoadWorkflowDetail(transactionId: string) {
    setBusy(true)
    setStatusText(`Loading workflow detail for ${transactionId}...`)
    try {
      const detail = await getDepositWorkflowDetail(transactionId)
      startTransition(() => {
        setSelectedWorkflow(detail)
        setSelectedTransactionId(transactionId)
        setActiveTab('workflows')
      })
      setStatusText(`Loaded workflow detail for ${transactionId}.`)
    } catch (error) {
      setStatusText(`Could not load workflow detail: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function handleRunDiagnostics() {
    setBusy(true)
    setStatusText(`Tracing correlation ${correlationQuery}...`)
    try {
      const response = await getCorrelationDiagnostics(correlationQuery.trim())
      startTransition(() => {
        setDiagnostics(response)
        setActiveTab('diagnostics')
      })
      setStatusText(`Correlation trace loaded for ${correlationQuery}.`)
    } catch (error) {
      setStatusText(`Could not load diagnostics: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function handleRetryCompensation(transactionId: string) {
    setBusy(true)
    setStatusText(`Requesting compensation retry for ${transactionId}...`)
    try {
      const action = await retryDepositCompensation(transactionId, 'Platform maintenance retry')
      startTransition(() => {
        setLastMaintenanceAction(action)
      })
      await loadOverview()
      await loadPlatformAudit()
      await handleLoadWorkflowDetail(transactionId)
      setStatusText(`Compensation retry requested for ${transactionId}.`)
    } catch (error) {
      setStatusText(`Could not request compensation retry: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function handleResolveReview(transactionId: string, resolution: DepositReviewResolutionOption) {
    setBusy(true)
    setStatusText(`Resolving pending review for ${transactionId}...`)
    try {
      const action = await resolveDepositReview(transactionId, resolution, maintenanceReason)
      startTransition(() => {
        setLastMaintenanceAction(action)
      })
      await loadOverview()
      await loadPlatformAudit()
      await handleLoadWorkflowDetail(transactionId)
      setStatusText(`Pending review resolved for ${transactionId}.`)
    } catch (error) {
      setStatusText(`Could not resolve pending review: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  async function handleRequeueOutbox(messageId: string) {
    setBusy(true)
    setStatusText(`Requeueing outbox message ${messageId}...`)
    try {
      const action = await requeueOutboxMessage(messageId, maintenanceReason)
      startTransition(() => {
        setLastMaintenanceAction(action)
      })
      await loadOverview()
      await loadPlatformAudit()
      setActiveTab('maintenance')
      setStatusText(`Outbox message ${messageId} requeued.`)
    } catch (error) {
      setStatusText(`Could not requeue outbox message: ${error instanceof Error ? error.message : String(error)}`)
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => {
    void handleRefresh()
  }, [])

  return (
    <main className="platform-shell">
      <section className="platform-hero">
        <div className="hero-card">
          <p className="eyebrow">Basic Banking System</p>
          <h1>Platform Operations Console</h1>
          <p className="hero-copy">
            A control-plane shell for service health, workflow monitoring, and correlation-driven
            diagnostics. This surface is intentionally separate from the business operations workspace.
          </p>
          <div className="hero-meta">
            <span className="meta-pill">Platform Read Scope</span>
            <span className="meta-pill">Gateway Aggregation</span>
            <span className="meta-pill">Workflow Diagnostics</span>
          </div>
        </div>

        <div className="signal-card">
          <div className="signal-header">
            <h2>Signal Board</h2>
            <button className="ghost-button" type="button" onClick={() => void handleRefresh()}>
              {busy ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
          <div className="signal-metrics">
            <div className="metric-tile">
              <strong>{overview?.services.length ?? 0}</strong>
              <span>services observed</span>
            </div>
            <div className="metric-tile">
              <strong>{workflowSummary?.pendingReviewCount ?? 0}</strong>
              <span>pending review items</span>
            </div>
            <div className="metric-tile">
              <strong>{workflowSummary?.failedCount ?? 0}</strong>
              <span>failed deposits</span>
            </div>
            <div className="metric-tile">
              <strong>{runtimeStatus?.pendingOutboxCount ?? 0}</strong>
              <span>pending outbox messages</span>
            </div>
            <div className="metric-tile">
              <strong>{diagnostics?.auditEvents.length ?? 0}</strong>
              <span>diagnostic audit hits</span>
            </div>
          </div>
          <p className="status-line">{statusText}</p>
        </div>
      </section>

      <section className="tab-row" aria-label="Platform operations tabs">
        {[
          ['overview', 'Overview'],
          ['services', 'Services'],
          ['workflows', 'Workflows'],
          ['diagnostics', 'Diagnostics'],
          ['maintenance', 'Maintenance'],
          ['audit', 'Audit'],
        ].map(([id, label]) => (
          <button
            key={id}
            type="button"
            className={activeTab === id ? 'tab-button tab-button-active' : 'tab-button'}
            onClick={() => setActiveTab(id as PlatformTab)}
          >
            {label}
          </button>
        ))}
      </section>

      <section className="surface-grid">
        {activeTab === 'overview' && (
          <>
            <article className="surface-card surface-card-wide">
              <h2>Platform Overview</h2>
              <p className="section-copy">
                This card is the current control-plane snapshot returned by the Gateway platform API.
              </p>
              <div className="summary-grid">
                <div className="summary-item">
                  <strong>{overview?.platform ?? 'n/a'}</strong>
                  <span>platform identifier</span>
                </div>
                <div className="summary-item">
                  <strong>{workflowSummary?.receivedCount ?? 0}</strong>
                  <span>received deposits</span>
                </div>
                <div className="summary-item">
                  <strong>{workflowSummary?.succeededCount ?? 0}</strong>
                  <span>succeeded deposits</span>
                </div>
                <div className="summary-item">
                  <strong>{formatTimestamp(overview?.checkedAt ?? null)}</strong>
                  <span>last platform check</span>
                </div>
              </div>
            </article>

            <article className="surface-card">
              <h2>Dependencies</h2>
              <div className="mini-table">
                {(overview?.dependencies ?? []).map((dependency) => (
                  <div key={dependency.name} className="row-card">
                    <div className="row-main">
                      <strong>{dependency.name}</strong>
                      <small>checked by {dependency.checkedBy}</small>
                    </div>
                    <div className="row-side">
                      <span className={buildStatusTone(dependency.status)}>{dependency.status}</span>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            <article className="surface-card">
              <h2>Runtime Workers</h2>
              <div className="mini-table">
                {(runtimeStatus?.workers ?? []).map((worker) => (
                  <div key={worker.workerName} className="row-card">
                    <div className="row-main">
                      <strong>{worker.workerName}</strong>
                      <small>{worker.mode}</small>
                    </div>
                    <div className="row-side">
                      <span className={buildStatusTone(worker.enabled ? 'Enabled' : 'Disabled')}>
                        {worker.enabled ? 'Enabled' : 'Disabled'}
                      </span>
                      <small>{worker.backlogCount} backlog / {worker.pollingIntervalMilliseconds}ms</small>
                    </div>
                  </div>
                ))}
                {!runtimeStatus?.workers.length && (
                  <p className="empty-state">No runtime worker signal is currently available.</p>
                )}
              </div>
            </article>

            <article className="surface-card">
              <h2>Review Queue</h2>
              <div className="mini-table">
                {pendingReviewItems.length === 0 ? (
                  <p className="empty-state">No pending review items are currently visible.</p>
                ) : (
                  pendingReviewItems.map((item) => (
                    <button
                      key={item.transactionId}
                      type="button"
                      className="row-card"
                      onClick={() => void handleLoadWorkflowDetail(item.transactionId)}
                    >
                      <div className="row-main">
                        <strong>{item.transactionNumber}</strong>
                        <small>{item.failureCode ?? 'No failure code'}</small>
                      </div>
                      <div className="row-side">
                        <span className={buildStatusTone(item.compensationStatus)}>{item.compensationStatus}</span>
                        <small>{item.accountNumber}</small>
                      </div>
                    </button>
                  ))
                )}
              </div>
            </article>
          </>
        )}

        {activeTab === 'services' && (
          <article className="surface-card surface-card-wide">
            <h2>Services</h2>
            <p className="section-copy">
              Service reachability is shown alongside deposit runtime worker state so the control plane can separate
              endpoint availability from background backlog pressure.
            </p>
            <div className="mini-table">
              {services.map((service) => (
                <div key={service.name} className="row-card">
                  <div className="row-main">
                    <strong>{service.name}</strong>
                    <small>{service.basePath}</small>
                  </div>
                  <div className="row-side">
                    <span className={buildStatusTone(service.health)}>{service.health}</span>
                    <small>
                      <a href={service.swaggerUrl}>Swagger</a> | <a href={service.openApiUrl}>OpenAPI</a>
                    </small>
                  </div>
                </div>
              ))}
            </div>
            <div className="summary-grid" style={{ marginTop: 20 }}>
              <div className="summary-item">
                <strong>{runtimeStatus?.messageTransport ?? 'n/a'}</strong>
                <span>deposit message transport</span>
              </div>
              <div className="summary-item">
                <strong>{runtimeStatus?.pendingReviewCount ?? 0}</strong>
                <span>runtime pending review</span>
              </div>
              <div className="summary-item">
                <strong>{runtimeStatus?.pendingOutboxCount ?? 0}</strong>
                <span>runtime pending outbox</span>
              </div>
              <div className="summary-item">
                <strong>{formatTimestamp(runtimeStatus?.checkedAt ?? null)}</strong>
                <span>runtime snapshot</span>
              </div>
            </div>
          </article>
        )}

        {activeTab === 'workflows' && (
          <>
            <article className="surface-card">
              <h2>Workflow Summary</h2>
              <div className="summary-grid">
                <div className="summary-item">
                  <strong>{workflowSummary?.receivedCount ?? 0}</strong>
                  <span>received</span>
                </div>
                <div className="summary-item">
                  <strong>{workflowSummary?.succeededCount ?? 0}</strong>
                  <span>succeeded</span>
                </div>
                <div className="summary-item">
                  <strong>{workflowSummary?.failedCount ?? 0}</strong>
                  <span>failed</span>
                </div>
                <div className="summary-item">
                  <strong>{workflowSummary?.pendingReviewCount ?? 0}</strong>
                  <span>pending review</span>
                </div>
              </div>
              <div className="control-row" style={{ marginTop: 16 }}>
                <input
                  className="search-input"
                  value={selectedTransactionId}
                  onChange={(event) => setSelectedTransactionId(event.target.value)}
                  placeholder="Enter a transaction id"
                />
                <button className="primary-button" type="button" onClick={() => void handleLoadWorkflowDetail(selectedTransactionId)}>
                  Load Workflow
                </button>
              </div>
            </article>

            <article className="surface-card">
              <h2>Selected Workflow</h2>
              {selectedWorkflow ? (
                <div className="stack-row">
                  <div className="detail-grid">
                    <dl><dt>Transaction</dt><dd>{selectedWorkflow.transactionNumber}</dd></dl>
                    <dl><dt>Status</dt><dd>{selectedWorkflow.status}</dd></dl>
                    <dl><dt>Correlation</dt><dd>{selectedWorkflow.correlationId}</dd></dl>
                    <dl><dt>Account Posting</dt><dd>{selectedWorkflow.accountPostingStatus}</dd></dl>
                    <dl><dt>Audit</dt><dd>{selectedWorkflow.auditStatus}</dd></dl>
                    <dl><dt>Compensation</dt><dd>{selectedWorkflow.compensationStatus}</dd></dl>
                    <dl><dt>Review</dt><dd>{selectedWorkflow.reviewResolution}</dd></dl>
                    <dl><dt>Failure Code</dt><dd>{selectedWorkflow.failureCode ?? 'n/a'}</dd></dl>
                    <dl><dt>Last Processed</dt><dd>{formatTimestamp(selectedWorkflow.lastProcessedAt)}</dd></dl>
                  </div>
                  <div className="control-row">
                    <input
                      className="search-input"
                      value={maintenanceReason}
                      onChange={(event) => setMaintenanceReason(event.target.value)}
                      placeholder="Reason for maintenance action"
                    />
                  </div>
                  <div className="control-row">
                    <button className="primary-button" type="button" onClick={() => void handleRetryCompensation(selectedWorkflow.transactionId)}>
                      Retry Compensation
                    </button>
                    <button className="ghost-button" type="button" onClick={() => void handleResolveReview(selectedWorkflow.transactionId, 'ReversedExternally')}>
                      Resolve As Reversed
                    </button>
                    <button className="ghost-button" type="button" onClick={() => void handleResolveReview(selectedWorkflow.transactionId, 'FailedExternally')}>
                      Resolve As Failed
                    </button>
                  </div>
                  {lastMaintenanceAction && (
                    <p className="section-copy">
                      Last maintenance action: {lastMaintenanceAction.actionType} by {lastMaintenanceAction.actorId} at{' '}
                      {formatTimestamp(lastMaintenanceAction.occurredAt)}.
                    </p>
                  )}
                </div>
              ) : (
                <p className="empty-state">Select a pending-review item or load a transaction id.</p>
              )}
            </article>
          </>
        )}

        {activeTab === 'maintenance' && (
          <>
            <article className="surface-card">
              <h2>Maintenance Queue</h2>
              <p className="section-copy">
                Select a pending review item, then use the workflow page to retry compensation or resolve it with an explicit reason.
              </p>
              <div className="mini-table">
                {pendingReviewItems.length ? (
                  pendingReviewItems.map((item) => (
                    <button
                      key={item.transactionId}
                      type="button"
                      className="row-card"
                      onClick={() => void handleLoadWorkflowDetail(item.transactionId)}
                    >
                      <div className="row-main">
                        <strong>{item.transactionNumber}</strong>
                        <small>{item.failureReason ?? 'No failure reason'}</small>
                      </div>
                      <div className="row-side">
                        <span className={buildStatusTone(item.compensationStatus)}>{item.compensationStatus}</span>
                        <small>{formatTimestamp(item.lastCompensationAttemptAt)}</small>
                      </div>
                    </button>
                  ))
                ) : (
                  <p className="empty-state">No pending review work items are currently available.</p>
                )}
              </div>
            </article>

            <article className="surface-card">
              <h2>Last Maintenance Action</h2>
              {lastMaintenanceAction ? (
                <div className="detail-grid">
                  <dl><dt>Action</dt><dd>{lastMaintenanceAction.actionType}</dd></dl>
                  <dl><dt>Target</dt><dd>{lastMaintenanceAction.targetId}</dd></dl>
                  <dl><dt>Actor</dt><dd>{lastMaintenanceAction.actorId}</dd></dl>
                  <dl><dt>Result</dt><dd>{lastMaintenanceAction.resultStatus}</dd></dl>
                  <dl><dt>Downstream Status</dt><dd>{lastMaintenanceAction.downstreamStatusCode}</dd></dl>
                  <dl><dt>Occurred</dt><dd>{formatTimestamp(lastMaintenanceAction.occurredAt)}</dd></dl>
                </div>
              ) : (
                <p className="empty-state">No platform maintenance action has been run in this session yet.</p>
              )}
            </article>

            <article className="surface-card surface-card-wide">
              <h2>Deposit Outbox</h2>
              <p className="section-copy">
                Inspect persisted outbox messages and requeue a message when supervised replay is needed.
              </p>
              <div className="mini-table">
                {outboxItems.length ? (
                  outboxItems.map((item) => (
                    <div key={item.messageId} className="row-card">
                      <div className="row-main">
                        <strong>{item.messageId}</strong>
                        <small>{item.transactionId} / {item.messageType}</small>
                      </div>
                      <div className="row-side">
                        <span className={buildStatusTone(item.processedAt ? 'Processed' : 'Pending')}>
                          {item.processedAt ? 'Processed' : 'Pending'}
                        </span>
                        <small>{formatTimestamp(item.occurredAt)}</small>
                        <button className="ghost-button" type="button" onClick={() => void handleRequeueOutbox(item.messageId)}>
                          Requeue
                        </button>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="empty-state">No deposit outbox messages are currently visible.</p>
                )}
              </div>
            </article>
          </>
        )}

        {activeTab === 'diagnostics' && (
          <>
            <article className="surface-card surface-card-wide">
              <h2>Correlation Diagnostics</h2>
              <div className="control-row">
                <input
                  className="search-input"
                  value={correlationQuery}
                  onChange={(event) => setCorrelationQuery(event.target.value)}
                  placeholder="Enter a correlation id"
                />
                <button className="primary-button" type="button" onClick={() => void handleRunDiagnostics()}>
                  Trace Correlation
                </button>
              </div>
              <p className="section-copy">
                This read-only view stitches together deposit workflow state and audit events for a shared correlation id.
              </p>
            </article>

            <article className="surface-card">
              <h2>Correlated Deposits</h2>
              <div className="mini-table">
                {diagnostics?.deposits.length ? (
                  diagnostics.deposits.map((deposit) => (
                    <div key={deposit.transactionId} className="row-card">
                      <div className="row-main">
                        <strong>{deposit.transactionNumber}</strong>
                        <small>{deposit.transactionId}</small>
                      </div>
                      <div className="row-side">
                        <span className={buildStatusTone(deposit.status)}>{deposit.status}</span>
                        <small>{deposit.failureCode ?? 'No failure code'}</small>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="empty-state">No deposits matched the current correlation id.</p>
                )}
              </div>
            </article>

            <article className="surface-card">
              <h2>Audit Events</h2>
              <div className="event-list">
                {diagnostics?.auditEvents.length ? (
                  diagnostics.auditEvents.map((event) => (
                    <div key={event.auditId} className="row-card">
                      <div className="row-main">
                        <strong>{event.action}</strong>
                        <small>{event.aggregateType} / {event.aggregateId}</small>
                      </div>
                      <div className="row-side">
                        <span className="pill pill-healthy">{event.actorId}</span>
                        <small>{formatTimestamp(event.occurredAt)}</small>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="empty-state">No audit events matched the current correlation id.</p>
                )}
              </div>
            </article>
          </>
        )}

        {activeTab === 'audit' && (
          <article className="surface-card surface-card-wide">
            <h2>Platform Audit Trail</h2>
            <p className="section-copy">
              This feed shows platform-side operational audit events, such as privileged maintenance actions.
            </p>
            <div className="event-list">
              {platformAudit.length ? (
                platformAudit.map((event) => (
                  <div key={event.auditId} className="row-card">
                    <div className="row-main">
                      <strong>{event.action}</strong>
                      <small>{event.aggregateType} / {event.aggregateId}</small>
                    </div>
                    <div className="row-side">
                      <span className="pill pill-warning">{event.actorId}</span>
                      <small>{formatTimestamp(event.occurredAt)}</small>
                    </div>
                  </div>
                ))
              ) : (
                <p className="empty-state">No platform audit events are currently available.</p>
              )}
            </div>
          </article>
        )}
      </section>
    </main>
  )
}

export default App
