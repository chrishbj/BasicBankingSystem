import type {
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  DepositResponse,
  DepositSummaryResponse,
  PagedResponse,
  PendingReviewDepositSummaryResponse,
  PendingReviewSortBy,
} from './types'

const apiKey = 'local-dev-api-key'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': apiKey,
      ...(init?.headers ?? {}),
    },
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `${response.status} ${response.statusText}`)
  }

  return response.json() as Promise<T>
}

export async function getHealth(basePath: string): Promise<string> {
  const response = await fetch(`${basePath}/api/v1/health`)
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`)
  }

  return response.text()
}

export function createCustomer(payload: Record<string, unknown>) {
  return request<CustomerResponse>('/customer-api/api/v1/customers', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function activateCustomer(customerId: string, reason: string) {
  return request<CustomerResponse>(`/customer-api/api/v1/customers/${customerId}/status`, {
    method: 'POST',
    body: JSON.stringify({
      targetStatus: 2,
      reason,
    }),
  })
}

export function getCustomers(pageNumber = 1, pageSize = 20) {
  return request<PagedResponse<CustomerResponse>>(
    `/customer-api/api/v1/customers?pageNumber=${pageNumber}&pageSize=${pageSize}`,
  )
}

export function openAccount(payload: Record<string, unknown>) {
  return request<AccountResponse>('/account-api/api/v1/accounts', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getAccount(accountId: string) {
  return request<AccountResponse>(`/account-api/api/v1/accounts/${accountId}`)
}

export function getAccountsByCustomer(customerId: string, pageNumber = 1, pageSize = 20) {
  return request<PagedResponse<AccountSummaryResponse>>(
    `/account-api/api/v1/accounts?customerId=${encodeURIComponent(customerId)}&pageNumber=${pageNumber}&pageSize=${pageSize}`,
  )
}

export function submitDeposit(payload: Record<string, unknown>, idempotencyKey: string, correlationId: string) {
  return request<DepositResponse>('/deposit-api/api/v1/deposits', {
    method: 'POST',
    headers: {
      'Idempotency-Key': idempotencyKey,
      'X-Correlation-Id': correlationId,
    },
    body: JSON.stringify(payload),
  })
}

export function getDeposit(transactionId: string) {
  return request<DepositResponse>(`/deposit-api/api/v1/deposits/${transactionId}`)
}

export function searchDeposits(params: URLSearchParams) {
  return request<PagedResponse<DepositSummaryResponse>>(`/deposit-api/api/v1/deposits?${params.toString()}`)
}

export function getPendingReview(sortBy: PendingReviewSortBy, descending: boolean) {
  return request<PagedResponse<PendingReviewDepositSummaryResponse>>(
    `/deposit-api/api/v1/deposits/review/pending?sortBy=${sortBy}&descending=${descending}`,
  )
}

export function retryPendingReview(transactionId: string, operatorId: string, note: string) {
  return request<DepositResponse>(`/deposit-api/api/v1/deposits/${transactionId}/review/retry-compensation`, {
    method: 'POST',
    body: JSON.stringify({
      operatorId,
      note,
    }),
  })
}

export function resolvePendingReview(
  transactionId: string,
  operatorId: string,
  note: string,
  resolution: 3 | 4,
) {
  return request<DepositResponse>(`/deposit-api/api/v1/deposits/${transactionId}/review/resolve`, {
    method: 'POST',
    body: JSON.stringify({
      resolution,
      operatorId,
      note,
    }),
  })
}
