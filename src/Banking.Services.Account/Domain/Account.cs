namespace Banking.Services.Account.Domain;

public sealed class Account
{
    public string AccountId { get; init; } = default!;
    public string AccountNumber { get; init; } = default!;
    public string CustomerId { get; init; } = default!;
    public string AccountType { get; init; } = default!;
    public string Currency { get; init; } = default!;
    public AccountStatus Status { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal LedgerBalance { get; set; }
    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; set; }
}
