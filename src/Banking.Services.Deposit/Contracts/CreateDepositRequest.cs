using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Contracts;

public sealed record CreateDepositRequest(
    string CustomerId,
    string AccountId,
    decimal Amount,
    string Currency,
    DepositChannel Channel,
    string? ReferenceNumber,
    string? Note);
