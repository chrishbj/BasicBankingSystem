using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Contracts;

public sealed record DepositResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    string? ReferenceNumber,
    DepositChannel Channel,
    DepositStatus Status,
    DepositSagaStepStatus AccountPostingStatus,
    DepositSagaStepStatus AuditStatus,
    DepositSagaStepStatus CompensationStatus,
    DepositReviewResolution ReviewResolution,
    string CorrelationId,
    string? FailureCode,
    string? FailureReason,
    int CompensationRetryCount,
    string? ReviewLastActionBy,
    string? ReviewNote,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt,
    DateTimeOffset? ReversedAt,
    DateTimeOffset? ReviewRequiredAt,
    DateTimeOffset? ReviewResolvedAt,
    DateTimeOffset? LastCompensationAttemptAt,
    DateTimeOffset? LastProcessedAt);

public sealed record DepositSummaryResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    string? ReferenceNumber,
    DepositChannel Channel,
    DepositStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt);
