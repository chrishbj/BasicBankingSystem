export type CustomerResponse = {
  customerNumber: string
  fullName: string
  identityType: string
  identityNumberMasked: string
  portalIdentityLast4: string
  mobile: string
  email?: string | null
  status: number
  riskLevel: string
  createdAt: string
  updatedAt: string
}

export type AccountResponse = {
  accountNumber: string
  accountType: string
  currency: string
  status: number
  availableBalance: number
  ledgerBalance: number
  openedAt: string
  closedAt?: string | null
}

export type AccountSummaryResponse = {
  accountNumber: string
  accountType: string
  currency: string
  status: number
  availableBalance: number
  ledgerBalance: number
}

export type AccountActivityResponse = {
  postingReference: string
  postingType: 1 | 2 | 3
  amount: number
  currency: string
  correlationId?: string | null
  reversalOfPostingReference?: string | null
  createdAt: string
}

export type DepositResponse = {
  transactionNumber: string
  accountNumber: string
  amount: number
  currency: string
  referenceNumber?: string | null
  status: number
  correlationId: string
  failureCode?: string | null
  failureReason?: string | null
  requestedAt: string
  postedAt?: string | null
}

export type DepositSummaryResponse = {
  transactionNumber: string
  accountNumber: string
  amount: number
  currency: string
  referenceNumber?: string | null
  status: number
  requestedAt: string
  postedAt?: string | null
}

export type PagedResponse<T> = {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type TransactionStatusSummaryResponse = {
  transactionNumber: string
  accountNumber: string
  amount: number
  currency: string
  status: number
  referenceNumber?: string | null
  requestedAt: string
  postedAt?: string | null
  failureCode?: string | null
  failureReason?: string | null
}

export type CustomerDashboardResponse = {
  customer: {
    customerNumber: string
    fullName: string
    status: number
    riskLevel: string
  }
  portfolio: {
    accountCount: number
    totalAvailableBalance: number
    totalLedgerBalance: number
  }
  currentAccount?: {
    accountNumber: string
    accountType: string
    status: number
    currency: string
    availableBalance: number
    ledgerBalance: number
  } | null
  latestActivity?: {
    type: string
    reference: string
    amount: number
    currency: string
    createdAt: string
  } | null
  recentActivities: Array<{
    accountNumber: string
    type: string
    reference: string
    amount: number
    currency: string
    createdAt: string
  }>
  recentTransactions: TransactionStatusSummaryResponse[]
}
