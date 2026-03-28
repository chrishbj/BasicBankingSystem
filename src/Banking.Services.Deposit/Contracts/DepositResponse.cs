using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Contracts;

public sealed record DepositResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    DepositChannel Channel,
    DepositStatus Status,
    DepositSagaStepStatus AccountPostingStatus,
    DepositSagaStepStatus AuditStatus,
    DepositSagaStepStatus CompensationStatus,
    string CorrelationId,
    string? FailureCode,
    string? FailureReason,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt,
    DateTimeOffset? ReversedAt,
    DateTimeOffset? LastProcessedAt);

public sealed record DepositSummaryResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    DepositChannel Channel,
    DepositStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt);
