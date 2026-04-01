namespace Banking.Bff.CustomerPortal.Contracts;

public sealed record CreateWithdrawalRequest(
    decimal Amount,
    string Currency,
    string ReferenceNumber,
    string? CorrelationId,
    string? Note);
