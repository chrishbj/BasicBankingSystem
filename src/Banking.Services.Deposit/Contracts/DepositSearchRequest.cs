using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Contracts;

public sealed record DepositSearchRequest(
    DepositStatus? Status,
    string? CorrelationId,
    string? FailureCode,
    DateTimeOffset? RequestedFrom,
    DateTimeOffset? RequestedTo);
