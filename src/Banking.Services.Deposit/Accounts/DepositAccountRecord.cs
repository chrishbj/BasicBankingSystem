namespace Banking.Services.Deposit.Accounts;

public sealed class DepositAccountRecord
{
    public string AccountId { get; init; } = default!;
    public string CustomerId { get; init; } = default!;
    public string Currency { get; init; } = default!;
    public DepositAccountStatus Status { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal LedgerBalance { get; set; }
}
