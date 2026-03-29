export type CustomerStatus = 1 | 2 | 3

export type DepositStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7

export type DepositReviewResolution = 1 | 2 | 3 | 4

export type PendingReviewSortBy = 'ReviewRequiredAt' | 'LastCompensationAttemptAt' | 'RequestedAt'

export type PagedResponse<T> = {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type CustomerResponse = {
  customerId: string
  customerNumber: string
  fullName: string
  identityType: string
  identityNumber: string
  mobile: string
  email: string
  status: CustomerStatus
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

export type DepositResponse = {
  transactionId: string
  transactionNumber: string
  customerId: string
  accountId: string
  amount: number
  currency: string
  channel: number
  status: DepositStatus
  accountPostingStatus: number
  auditStatus: number
  compensationStatus: number
  reviewResolution: DepositReviewResolution
  correlationId: string
  failureCode?: string | null
  failureReason?: string | null
  compensationRetryCount: number
  reviewLastActionBy?: string | null
  reviewNote?: string | null
  requestedAt: string
  postedAt?: string | null
  reversedAt?: string | null
  reviewRequiredAt?: string | null
  reviewResolvedAt?: string | null
  lastCompensationAttemptAt?: string | null
  lastProcessedAt?: string | null
}

export type DepositSummaryResponse = {
  transactionId: string
  transactionNumber: string
  customerId: string
  accountId: string
  amount: number
  currency: string
  channel: number
  status: DepositStatus
  requestedAt: string
  postedAt?: string | null
}

export type PendingReviewDepositSummaryResponse = {
  transactionId: string
  transactionNumber: string
  customerId: string
  accountId: string
  amount: number
  currency: string
  compensationStatus: number
  reviewResolution: DepositReviewResolution
  failureCode?: string | null
  failureReason?: string | null
  compensationRetryCount: number
  reviewLastActionBy?: string | null
  reviewNote?: string | null
  requestedAt: string
  reviewRequiredAt?: string | null
  lastCompensationAttemptAt?: string | null
  lastProcessedAt?: string | null
}
