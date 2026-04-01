namespace Banking.Bff.CustomerPortal.Contracts;

public sealed record AccountResponse(
    string AccountId,
    string AccountNumber,
    string CustomerId,
    string AccountType,
    string Currency,
    int Status,
    decimal AvailableBalance,
    decimal LedgerBalance,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);

public sealed record AccountSummaryResponse(
    string AccountId,
    string AccountNumber,
    string AccountType,
    string Currency,
    int Status,
    decimal AvailableBalance,
    decimal LedgerBalance);

public sealed record AccountActivityResponse(
    string PostingReference,
    string AccountId,
    int PostingType,
    decimal Amount,
    string Currency,
    string? CorrelationId,
    string? ReversalOfPostingReference,
    DateTimeOffset CreatedAt);
