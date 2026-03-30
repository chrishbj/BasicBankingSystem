import type {
  AccountActivityResponse,
  AccountResponse,
  AccountSummaryResponse,
  CustomerResponse,
  DepositResponse,
  DepositSummaryResponse,
  PagedResponse,
} from './types'

const apiKey = 'local-dev-api-key'

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
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': apiKey,
      ...(init?.headers ?? {}),
    },
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(extractErrorMessage(text, response))
  }

  return response.json() as Promise<T>
}

export function getCustomers(pageNumber = 1, pageSize = 50) {
  return request<PagedResponse<CustomerResponse>>(`/customer-api/api/v1/customers?pageNumber=${pageNumber}&pageSize=${pageSize}`)
}

export function signInCustomer(customerNumber: string, identityLast4: string) {
  return request<CustomerResponse>('/customer-api/api/v1/customers/portal-sign-in', {
    method: 'POST',
    body: JSON.stringify({
      customerNumber,
      identityLast4,
    }),
  })
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

export function submitWithdrawal(accountId: string, payload: Record<string, unknown>) {
  return request<AccountResponse>(`/account-api/api/v1/accounts/${accountId}/withdrawals`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getDeposit(transactionId: string) {
  return request<DepositResponse>(`/deposit-api/api/v1/deposits/${transactionId}`)
}

export function searchDeposits(customerId: string, accountId?: string, pageNumber = 1, pageSize = 20) {
  const query = new URLSearchParams({
    customerId,
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  })

  if (accountId) {
    query.set('accountId', accountId)
  }

  return request<PagedResponse<DepositSummaryResponse>>(`/deposit-api/api/v1/deposits?${query.toString()}`)
}
