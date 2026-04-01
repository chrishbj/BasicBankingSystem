using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Contracts;

public sealed record PendingReviewDepositSummaryResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    DepositSagaStepStatus CompensationStatus,
    DepositReviewResolution ReviewResolution,
    string? FailureCode,
    string? FailureReason,
    int CompensationRetryCount,
    string? ReviewLastActionBy,
    string? ReviewNote,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ReviewRequiredAt,
    DateTimeOffset? LastCompensationAttemptAt,
    DateTimeOffset? LastProcessedAt);
