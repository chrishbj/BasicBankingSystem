using System.Collections.Concurrent;

namespace Banking.Services.Account.Repositories;

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<string, Domain.Account> _accounts = new();

    public Task AddAsync(Domain.Account account, CancellationToken cancellationToken)
    {
        _accounts[account.AccountId] = account;
        return Task.CompletedTask;
    }

    public Task<Domain.Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken)
    {
        _accounts.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public Task<IReadOnlyCollection<Domain.Account>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Domain.Account>>(
            _accounts.Values
                .Where(account => string.Equals(account.CustomerId, customerId, StringComparison.Ordinal))
                .OrderByDescending(account => account.OpenedAt)
                .ToArray());
    }

    public Task UpdateAsync(Domain.Account account, CancellationToken cancellationToken)
    {
        _accounts[account.AccountId] = account;
        return Task.CompletedTask;
    }
}
