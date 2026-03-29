import type {
  AccountActivityResponse,
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  PagedResponse,
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

export function getCustomers(pageNumber = 1, pageSize = 50) {
  return request<PagedResponse<CustomerResponse>>(`/customer-api/api/v1/customers?pageNumber=${pageNumber}&pageSize=${pageSize}`)
}

export function getAccountsByCustomer(customerId: string, pageNumber = 1, pageSize = 50) {
  return request<PagedResponse<AccountSummaryResponse>>(
    `/account-api/api/v1/accounts?customerId=${encodeURIComponent(customerId)}&pageNumber=${pageNumber}&pageSize=${pageSize}`,
  )
}

export function getAccount(accountId: string) {
  return request<AccountResponse>(`/account-api/api/v1/accounts/${accountId}`)
}

export function getAccountActivities(accountId: string, pageNumber = 1, pageSize = 50) {
  return request<PagedResponse<AccountActivityResponse>>(
    `/account-api/api/v1/accounts/${accountId}/activities?pageNumber=${pageNumber}&pageSize=${pageSize}`,
  )
}
