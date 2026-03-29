export type CustomerResponse = {
  customerId: string
  customerNumber: string
  fullName: string
  identityType: string
  identityNumber: string
  mobile: string
  email: string
  status: number
  riskLevel: string
  createdAt: string
}

export type AccountResponse = {
  accountId: string
  accountNumber: string
  customerId: string
  accountType: string
  currency: string
  status: number
  availableBalance: number
  ledgerBalance: number
  openedAt: string
  closedAt?: string | null
}

export type AccountSummaryResponse = {
  accountId: string
  accountNumber: string
  accountType: string
  currency: string
  status: number
  availableBalance: number
  ledgerBalance: number
}

export type AccountActivityResponse = {
  postingReference: string
  accountId: string
  postingType: 1 | 2 | 3
  amount: number
  currency: string
  correlationId?: string | null
  reversalOfPostingReference?: string | null
  createdAt: string
}

export type PagedResponse<T> = {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}
