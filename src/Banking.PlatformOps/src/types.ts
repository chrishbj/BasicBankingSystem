export type PlatformServiceStatus = {
  name: string
  basePath: string
  health: string
  statusCode: number | null
  swaggerUrl: string
  openApiUrl: string
}

export type PlatformDependencyStatus = {
  name: string
  status: string
  checkedBy: string
}

export type PlatformCompatibilityStatus = {
  serviceName: string
  environment: string
  surface: string
  baseline: string
  runtimeOpenApiUrl: string
  parseable: boolean
  status: string
  driftSummary: string
  runtimeTitle?: string | null
  runtimeVersion?: string | null
  runtimePathCount: number
  expectedCriticalPathCount: number
  missingCriticalPathCount: number
  missingCriticalPaths: string[]
  parseError?: string | null
  lastVerifiedAt: string
}

export type PlatformRolloutStatus = {
  serviceName: string
  environment: string
  currentVersion: string
  targetVersion: string
  stage: string
  canaryPercent: number
  healthStatus: string
  compatibilityStatus: string
  lastUpdatedAt: string
}

export type PlatformEnvironmentSummary = {
  environment: string
  gateway: string
  checkedAt: string
  serviceCount: number
  healthyServiceCount: number
  publicContractBaseline: string
  platformSurfaceBaseline: string
  comparisonReady: boolean
  notes: string
}

export type DepositPendingReviewItem = {
  transactionId: string
  transactionNumber: string
  customerId: string
  accountId: string
  accountNumber: string
  amount: number
  currency: string
  compensationStatus: string
  reviewResolution: string
  failureCode: string | null
  failureReason: string | null
  compensationRetryCount: number
  reviewLastActionBy: string | null
  reviewNote: string | null
  requestedAt: string
  reviewRequiredAt: string | null
  lastCompensationAttemptAt: string | null
  lastProcessedAt: string | null
}

export type DepositOutboxMessageItem = {
  messageId: string
  transactionId: string
  messageType: string
  occurredAt: string
  processedAt: string | null
  lastError: string | null
}

export type DepositWorkerRuntimeStatus = {
  workerName: string
  mode: string
  enabled: boolean
  pollingIntervalMilliseconds: number
  backlogCount: number
  notes: string
}

export type DepositRuntimeStatus = {
  checkedAt: string
  messageTransport: string
  pendingReviewCount: number
  pendingOutboxCount: number
  workers: DepositWorkerRuntimeStatus[]
}

export type DepositWorkflowSummary = {
  checkedAt: string
  receivedCount: number
  succeededCount: number
  failedCount: number
  pendingReviewCount: number
  pendingReviewItems: DepositPendingReviewItem[]
}

export type DepositWorkflowDetail = {
  transactionId: string
  transactionNumber: string
  customerId: string
  accountId: string
  amount: number
  currency: string
  referenceNumber: string | null
  channel: string
  status: string
  accountPostingStatus: string
  auditStatus: string
  compensationStatus: string
  reviewResolution: string
  correlationId: string
  failureCode: string | null
  failureReason: string | null
  compensationRetryCount: number
  reviewLastActionBy: string | null
  reviewNote: string | null
  requestedAt: string
  postedAt: string | null
  reversedAt: string | null
  reviewRequiredAt: string | null
  reviewResolvedAt: string | null
  lastCompensationAttemptAt: string | null
  lastProcessedAt: string | null
}

export type AuditTraceItem = {
  auditId: string
  actorType: string
  actorId: string
  action: string
  aggregateType: string
  aggregateId: string
  correlationId: string
  occurredAt: string
}

export type CorrelationDiagnostics = {
  correlationId: string
  checkedAt: string
  deposits: DepositWorkflowDetail[]
  auditEvents: AuditTraceItem[]
}

export type PlatformMaintenanceAction = {
  operationId: string
  actionType: string
  targetType: string
  targetId: string
  actorId: string
  resultStatus: string
  downstreamStatusCode: number
  reason: string
  occurredAt: string
}

export type DepositReviewResolutionOption = 'ReversedExternally' | 'FailedExternally'

export type PlatformOverview = {
  platform: string
  checkedAt: string
  services: PlatformServiceStatus[]
  dependencies: PlatformDependencyStatus[]
  deposits: DepositWorkflowSummary
  depositRuntime: DepositRuntimeStatus
}
