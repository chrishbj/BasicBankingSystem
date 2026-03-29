using Banking.Services.Account.Domain;

namespace Banking.Services.Account.Contracts;

public sealed record AccountActivityResponse(
    string PostingReference,
    string AccountId,
    AccountPostingType PostingType,
    decimal Amount,
    string Currency,
    string? CorrelationId,
    string? ReversalOfPostingReference,
    DateTimeOffset CreatedAt);
