namespace Banking.Bff.CustomerPortal.Contracts;

public sealed record DepositResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    string? ReferenceNumber,
    int Status,
    string CorrelationId,
    string? FailureCode,
    string? FailureReason,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt);

public sealed record DepositSummaryResponse(
    string TransactionId,
    string TransactionNumber,
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    string? ReferenceNumber,
    int Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? PostedAt);

public sealed record CreatePortalDepositRequest(
    string AccountNumber,
    decimal Amount,
    string Currency,
    int Channel,
    string? ReferenceNumber,
    string? Note);
