using System.Collections.Concurrent;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;

namespace Banking.Services.Deposit.Repositories;

public class InMemoryDepositRepository : IDepositRepository
{
    private readonly ConcurrentDictionary<string, DepositTransaction> _transactions = new();
    private readonly ConcurrentDictionary<string, DepositOutboxMessage> _outboxMessages = new();

    public Task AddAsync(DepositTransaction transaction, DepositOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        _transactions[transaction.TransactionId] = transaction;
        _outboxMessages[outboxMessage.MessageId] = outboxMessage;
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

    public Task<IReadOnlyCollection<DepositOutboxMessage>> GetPendingOutboxMessagesAsync(int maxCount, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<DepositOutboxMessage>>(
            _outboxMessages.Values
                .Where(item => item.ProcessedAt is null)
                .OrderBy(item => item.OccurredAt)
                .Take(maxCount)
                .ToArray());
    }

    public Task MarkOutboxMessageProcessedAsync(string messageId, DateTimeOffset processedAt, CancellationToken cancellationToken)
    {
        if (_outboxMessages.TryGetValue(messageId, out var message))
        {
            message.ProcessedAt = processedAt;
            message.LastError = null;
        }

        return Task.CompletedTask;
    }

    public virtual Task UpdateAsync(DepositTransaction transaction, CancellationToken cancellationToken)
    {
        _transactions[transaction.TransactionId] = transaction;
        return Task.CompletedTask;
    }
}
