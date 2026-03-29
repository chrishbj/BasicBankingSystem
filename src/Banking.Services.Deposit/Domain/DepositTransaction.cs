namespace Banking.Services.Deposit.Domain;

public sealed class DepositTransaction
{
    public string TransactionId { get; init; } = default!;
    public string TransactionNumber { get; init; } = default!;
    public string CustomerId { get; init; } = default!;
    public string AccountId { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string? ReferenceNumber { get; init; }
    public DepositChannel Channel { get; init; }
    public DepositStatus Status { get; set; }
    public DepositSagaStepStatus AccountPostingStatus { get; set; }
    public DepositSagaStepStatus AuditStatus { get; set; }
    public DepositSagaStepStatus CompensationStatus { get; set; }
    public DepositReviewResolution ReviewResolution { get; set; }
    public string IdempotencyKey { get; init; } = default!;
    public string CorrelationId { get; init; } = default!;
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
    public int CompensationRetryCount { get; set; }
    public string? ReviewLastActionBy { get; set; }
    public string? ReviewNote { get; set; }
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? PostedAt { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public DateTimeOffset? ReviewRequiredAt { get; set; }
    public DateTimeOffset? ReviewResolvedAt { get; set; }
    public DateTimeOffset? LastCompensationAttemptAt { get; set; }
    public DateTimeOffset? LastProcessedAt { get; set; }
}
