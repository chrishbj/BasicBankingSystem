import type {
  AccountActivityResponse,
  AccountResponse,
  AccountSummaryResponse,
  CustomerDashboardResponse,
  CustomerResponse,
  DepositResponse,
  DepositSummaryResponse,
  PagedResponse,
  TransactionStatusSummaryResponse,
} from './types'

function extractErrorMessage(rawText: string, response: Response) {
  if (!rawText) {
    return `${response.status} ${response.statusText}`
  }

  try {
    const parsed = JSON.parse(rawText) as { title?: string; detail?: string; status?: number }
    if (parsed.title || parsed.detail) {
      return [parsed.title, parsed.detail].filter(Boolean).join(': ')
    }
  } catch {
    // Fall back to raw text when the payload is not JSON.
  }

  return rawText
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(extractErrorMessage(text, response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export function signInCustomer(customerNumber: string, identityLast4: string) {
  return request<CustomerResponse>('/customer-portal-api/api/customer-portal/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({
      customerNumber,
      identityLast4,
    }),
  })
}

export function signOutCustomer() {
  return request<void>('/customer-portal-api/api/customer-portal/auth/sign-out', {
    method: 'POST',
  })
}

export function getCurrentCustomer() {
  return request<CustomerResponse>('/customer-portal-api/api/customer-portal/auth/me')
}

export function getDashboard() {
  return request<CustomerDashboardResponse>('/customer-portal-api/api/customer-portal/dashboard')
}

export function getAccountsByCustomer(pageNumber = 1, pageSize = 50) {
  return request<PagedResponse<AccountSummaryResponse>>(
    `/customer-portal-api/api/customer-portal/accounts?pageNumber=${pageNumber}&pageSize=${pageSize}`,
  )
}

export function getAccount(accountNumber: string) {
  return request<AccountResponse>(`/customer-portal-api/api/customer-portal/accounts/${encodeURIComponent(accountNumber)}`)
}

export function getAccountActivities(accountNumber: string, pageNumber = 1, pageSize = 50) {
  return request<PagedResponse<AccountActivityResponse>>(
    `/customer-portal-api/api/customer-portal/accounts/${encodeURIComponent(accountNumber)}/activities?pageNumber=${pageNumber}&pageSize=${pageSize}`,
  )
}

export function submitDeposit(payload: Record<string, unknown>, _idempotencyKey: string, _correlationId: string) {
  return request<DepositResponse>('/customer-portal-api/api/customer-portal/deposits', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function submitWithdrawal(accountId: string, payload: Record<string, unknown>) {
  return request<AccountResponse>(`/customer-portal-api/api/customer-portal/accounts/${encodeURIComponent(accountId)}/withdrawals`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getDeposit(transactionNumber: string) {
  return request<TransactionStatusSummaryResponse>(`/customer-portal-api/api/customer-portal/transactions/${encodeURIComponent(transactionNumber)}`)
}

export function searchDeposits(accountNumber?: string, pageNumber = 1, pageSize = 20) {
  const query = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  })

  if (accountNumber) {
    query.set('accountNumber', accountNumber)
  }

  return request<PagedResponse<DepositSummaryResponse>>(`/customer-portal-api/api/customer-portal/deposits?${query.toString()}`)
}

export function getTransactions(accountNumber?: string, pageNumber = 1, pageSize = 20) {
  const query = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  })

  if (accountNumber) {
    query.set('accountNumber', accountNumber)
  }

  return request<PagedResponse<TransactionStatusSummaryResponse>>(`/customer-portal-api/api/customer-portal/transactions?${query.toString()}`)
}
