import type {
  CorrelationDiagnostics,
  DepositOutboxMessageItem,
  DepositRuntimeStatus,
  DepositReviewResolutionOption,
  DepositPendingReviewItem,
  DepositWorkflowDetail,
  DepositWorkflowSummary,
  PlatformCompatibilityStatus,
  PlatformEnvironmentSummary,
  PlatformMaintenanceAction,
  PlatformOverview,
  PlatformRolloutStatus,
  PlatformServiceStatus,
  AuditTraceItem,
} from './types'

const apiKey = 'local-dev-api-key'

async function request<T>(path: string): Promise<T> {
  const response = await fetch(path, {
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': apiKey,
    },
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `${response.status} ${response.statusText}`)
  }

  return response.json() as Promise<T>
}

export function getPlatformOverview() {
  return request<PlatformOverview>('/gateway-api/api/platform/overview')
}

export function getPlatformServices() {
  return request<PlatformServiceStatus[]>('/gateway-api/api/platform/services')
}

export function getPlatformCompatibility() {
  return request<PlatformCompatibilityStatus[]>('/gateway-api/api/platform/compatibility')
}

export function getPlatformRollouts() {
  return request<PlatformRolloutStatus[]>('/gateway-api/api/platform/rollouts')
}

export function getPlatformEnvironments() {
  return request<PlatformEnvironmentSummary[]>('/gateway-api/api/platform/environments')
}

export function getDepositWorkflowSummary() {
  return request<DepositWorkflowSummary>('/gateway-api/api/platform/workflows/deposits/summary')
}

export function getPendingReviewItems() {
  return request<DepositPendingReviewItem[]>('/gateway-api/api/platform/workflows/deposits/pending-review')
}

export function getDepositOutboxMessages() {
  return request<DepositOutboxMessageItem[]>('/gateway-api/api/platform/workflows/deposits/outbox')
}

export function getDepositRuntimeStatus() {
  return request<DepositRuntimeStatus>('/gateway-api/api/platform/workflows/deposits/runtime')
}

export function getDepositWorkflowDetail(transactionId: string) {
  return request<DepositWorkflowDetail>(`/gateway-api/api/platform/workflows/deposits/${encodeURIComponent(transactionId)}`)
}

export function getCorrelationDiagnostics(correlationId: string) {
  return request<CorrelationDiagnostics>(`/gateway-api/api/platform/diagnostics/correlation/${encodeURIComponent(correlationId)}`)
}

export function retryDepositCompensation(transactionId: string, reason: string) {
  return fetch(`/gateway-api/api/platform/maintenance/deposits/${encodeURIComponent(transactionId)}/retry-compensation`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': apiKey,
    },
    body: JSON.stringify({ reason }),
  }).then(async (response) => {
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `${response.status} ${response.statusText}`)
    }

    return response.json() as Promise<PlatformMaintenanceAction>
  })
}

export function getPlatformOperationsAudit() {
  return request<AuditTraceItem[]>('/gateway-api/api/platform/audit/operations')
}

export function resolveDepositReview(
  transactionId: string,
  resolution: DepositReviewResolutionOption,
  reason: string,
) {
  return fetch(`/gateway-api/api/platform/maintenance/deposits/${encodeURIComponent(transactionId)}/resolve-review`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': apiKey,
    },
    body: JSON.stringify({ resolution, reason }),
  }).then(async (response) => {
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `${response.status} ${response.statusText}`)
    }

    return response.json() as Promise<PlatformMaintenanceAction>
  })
}

export function requeueOutboxMessage(messageId: string, reason: string) {
  return fetch(`/gateway-api/api/platform/maintenance/deposits/outbox/${encodeURIComponent(messageId)}/requeue`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': apiKey,
    },
    body: JSON.stringify({ reason }),
  }).then(async (response) => {
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `${response.status} ${response.statusText}`)
    }

    return response.json() as Promise<PlatformMaintenanceAction>
  })
}
