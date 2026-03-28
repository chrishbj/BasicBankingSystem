namespace Banking.Services.Account.Contracts;

public sealed record ReverseDepositRequest(
    string ReversalReference,
    string OriginalPostingReference,
    decimal Amount,
    string Currency,
    string? CorrelationId,
    string Reason);
