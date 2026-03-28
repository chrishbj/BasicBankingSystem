namespace Banking.Services.Deposit.Accounts;

public sealed record ReverseDepositRequest(
    string ReversalReference,
    string OriginalPostingReference,
    decimal Amount,
    string Currency,
    string? CorrelationId,
    string Reason);
