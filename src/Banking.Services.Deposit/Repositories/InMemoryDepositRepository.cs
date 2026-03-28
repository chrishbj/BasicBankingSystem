using System.Collections.Concurrent;
using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Repositories;

public sealed class InMemoryDepositRepository : IDepositRepository
{
    private readonly ConcurrentDictionary<string, DepositTransaction> _transactions = new();

    public Task AddAsync(DepositTransaction transaction, CancellationToken cancellationToken)
    {
        _transactions[transaction.TransactionId] = transaction;
        return Task.CompletedTask;
    }

    public Task<DepositTransaction?> GetByIdAsync(string transactionId, CancellationToken cancellationToken)
    {
        _transactions.TryGetValue(transactionId, out var transaction);
        return Task.FromResult(transaction);
    }

    public Task<DepositTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var transaction = _transactions.Values.FirstOrDefault(item =>
            string.Equals(item.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

        return Task.FromResult(transaction);
    }

    public Task<IReadOnlyCollection<DepositTransaction>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<DepositTransaction>>(
            _transactions.Values
                .OrderByDescending(item => item.RequestedAt)
                .ToArray());
    }

    public Task UpdateAsync(DepositTransaction transaction, CancellationToken cancellationToken)
    {
        _transactions[transaction.TransactionId] = transaction;
        return Task.CompletedTask;
    }
}
