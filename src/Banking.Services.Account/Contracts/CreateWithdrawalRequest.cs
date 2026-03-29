namespace Banking.Services.Account.Contracts;

public sealed record CreateWithdrawalRequest(
    decimal Amount,
    string Currency,
    string ReferenceNumber,
    string? CorrelationId,
    string? Note);
