using System.Collections.Concurrent;

namespace Banking.Services.Deposit.Accounts;

public sealed class InMemoryDepositAccountDirectory : IDepositAccountDirectory
{
    private readonly ConcurrentDictionary<string, DepositAccountRecord> _accounts =
        new(new[]
        {
            new KeyValuePair<string, DepositAccountRecord>(
                "acc_active_001",
                new DepositAccountRecord
                {
                    AccountId = "acc_active_001",
                    CustomerId = "cus_active_001",
                    Currency = "CNY",
                    Status = DepositAccountStatus.Active,
                    AvailableBalance = 0m,
                    LedgerBalance = 0m
                }),
            new KeyValuePair<string, DepositAccountRecord>(
                "acc_frozen_001",
                new DepositAccountRecord
                {
                    AccountId = "acc_frozen_001",
                    CustomerId = "cus_active_001",
                    Currency = "CNY",
                    Status = DepositAccountStatus.Frozen,
                    AvailableBalance = 0m,
                    LedgerBalance = 0m
                })
        });

    public Task<DepositAccountRecord?> GetByIdAsync(string accountId, CancellationToken cancellationToken)
    {
        _accounts.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public Task PostDepositAsync(string accountId, decimal amount, CancellationToken cancellationToken)
    {
        if (_accounts.TryGetValue(accountId, out var account))
        {
            account.AvailableBalance += amount;
            account.LedgerBalance += amount;
        }

        return Task.CompletedTask;
    }
}
