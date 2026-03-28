using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;

namespace Banking.Services.Deposit.Repositories;

public interface IDepositRepository
{
    Task AddAsync(DepositTransaction transaction, DepositOutboxMessage outboxMessage, CancellationToken cancellationToken);
    Task<DepositTransaction?> GetByIdAsync(string transactionId, CancellationToken cancellationToken);
    Task<DepositTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DepositTransaction>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DepositTransaction>> GetPendingReviewAsync(int maxCount, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DepositOutboxMessage>> GetPendingOutboxMessagesAsync(int maxCount, CancellationToken cancellationToken);
    Task MarkOutboxMessageProcessedAsync(string messageId, DateTimeOffset processedAt, CancellationToken cancellationToken);
    Task UpdateAsync(DepositTransaction transaction, CancellationToken cancellationToken);
}
