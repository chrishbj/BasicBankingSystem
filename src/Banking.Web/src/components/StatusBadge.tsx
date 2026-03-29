import type { DepositResponse, DepositStatus, DepositReviewResolution, PendingReviewDepositSummaryResponse } from '../types'

type StatusBadgeProps = {
  label: string
  tone: 'neutral' | 'success' | 'warning' | 'danger' | 'info'
}

export type StatusTone = StatusBadgeProps['tone']

function toneClassName(tone: StatusBadgeProps['tone']) {
  return `status-badge status-badge-${tone}`
}

export function StatusBadge({ label, tone }: StatusBadgeProps) {
  return <span className={toneClassName(tone)}>{label}</span>
}

export function getDepositStatusLabel(status: DepositStatus) {
  switch (status) {
    case 1:
      return 'Received'
    case 2:
      return 'Processing'
    case 3:
      return 'Succeeded'
    case 4:
      return 'Rejected'
    case 5:
      return 'Failed'
    case 6:
      return 'Pending Review'
    case 7:
      return 'Reversed'
    default:
      return `Status ${status}`
  }
}

export function getDepositStatusTone(status: DepositStatus): StatusTone {
  switch (status) {
    case 3:
    case 7:
      return 'success'
    case 6:
      return 'warning'
    case 5:
    case 4:
      return 'danger'
    case 2:
      return 'info'
    default:
      return 'neutral'
  }
}

export function getReviewResolutionLabel(resolution: DepositReviewResolution) {
  switch (resolution) {
    case 1:
      return 'None'
    case 2:
      return 'Retry Requested'
    case 3:
      return 'Reversed Externally'
    case 4:
      return 'Failed Externally'
    default:
      return `Resolution ${resolution}`
  }
}

export function buildDepositBadge(deposit: DepositResponse | PendingReviewDepositSummaryResponse) {
  if ('status' in deposit) {
    return {
      label: getDepositStatusLabel(deposit.status),
      tone: getDepositStatusTone(deposit.status),
    } as const
  }

  return {
    label: getReviewResolutionLabel(deposit.reviewResolution),
    tone: deposit.reviewResolution === 3
      ? 'success'
      : deposit.reviewResolution === 4
        ? 'danger'
        : 'warning',
  } as const
}
