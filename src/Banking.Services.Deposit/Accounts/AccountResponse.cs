namespace Banking.Services.Deposit.Accounts;

public sealed record AccountResponse(
    string AccountId,
    string AccountNumber,
    string CustomerId,
    string AccountType,
    string Currency,
    string Status,
    decimal AvailableBalance,
    decimal LedgerBalance,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);
