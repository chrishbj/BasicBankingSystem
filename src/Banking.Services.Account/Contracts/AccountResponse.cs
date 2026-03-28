using Banking.Services.Account.Domain;

namespace Banking.Services.Account.Contracts;

public sealed record AccountResponse(
    string AccountId,
    string AccountNumber,
    string CustomerId,
    string AccountType,
    string Currency,
    AccountStatus Status,
    decimal AvailableBalance,
    decimal LedgerBalance,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);

public sealed record AccountSummaryResponse(
    string AccountId,
    string AccountNumber,
    string AccountType,
    string Currency,
    AccountStatus Status,
    decimal AvailableBalance,
    decimal LedgerBalance);
