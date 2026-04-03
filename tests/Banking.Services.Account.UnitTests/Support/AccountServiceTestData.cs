using Banking.Services.Account.Domain;

namespace Banking.Services.Account.UnitTests.Support;

internal static class AccountServiceTestData
{
    public static Banking.Services.Account.Domain.Account CreateAccount(
        string accountId = "acc_active_001",
        string accountNumber = "6222202604029999",
        decimal balance = 0m,
        AccountStatus status = AccountStatus.Active)
        => new()
        {
            AccountId = accountId,
            AccountNumber = accountNumber,
            CustomerId = "cus_active_001",
            AccountType = "Checking",
            Currency = "USD",
            Status = status,
            AvailableBalance = balance,
            LedgerBalance = balance,
            OpenedAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)
        };

    public static Banking.Services.Account.Domain.AccountPosting CreatePosting(
        string postingReference,
        AccountPostingType postingType,
        decimal amount,
        string accountId = "acc_active_001",
        string currency = "USD")
        => new()
        {
            PostingReference = postingReference,
            AccountId = accountId,
            PostingType = postingType,
            Amount = amount,
            Currency = currency,
            CreatedAt = new DateTimeOffset(2026, 4, 2, 10, 5, 0, TimeSpan.Zero)
        };
}
