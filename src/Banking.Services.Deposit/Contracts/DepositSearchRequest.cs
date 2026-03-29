using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Contracts;

public sealed record DepositSearchRequest(
    DepositStatus? Status,
    string? CustomerId,
    string? AccountId,
    string? CorrelationId,
    string? FailureCode,
    DateTimeOffset? RequestedFrom,
    DateTimeOffset? RequestedTo);
