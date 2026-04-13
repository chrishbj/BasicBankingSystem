using Banking.Services.Deposit.Data;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Deposit.Repositories;

public sealed class EfDepositRepository(DepositDbContext dbContext) : IDepositRepository
{
    public async Task AddAsync(DepositTransaction transaction, DepositOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        dbContext.Deposits.Add(transaction);
        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<DepositTransaction?> GetByIdAsync(string transactionId, CancellationToken cancellationToken)
    {
        return dbContext.Deposits.FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);
    }

    public Task<DepositTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return dbContext.Deposits.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DepositTransaction>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.Deposits.ToListAsync(cancellationToken);
        return items
            .OrderByDescending(x => x.RequestedAt)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DepositTransaction>> GetPendingReviewAsync(int maxCount, CancellationToken cancellationToken)
    {
        var items = await dbContext.Deposits.ToListAsync(cancellationToken);

        return items
            .Where(x => x.Status == DepositStatus.PendingReview)
            .OrderBy(x => x.LastCompensationAttemptAt ?? x.ReviewRequiredAt ?? x.RequestedAt)
            .Take(maxCount)
            .ToArray();
    }

    public Task<DepositOutboxMessage?> GetOutboxMessageByIdAsync(string messageId, CancellationToken cancellationToken)
    {
        return dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DepositOutboxMessage>> GetOutboxMessagesAsync(
        int maxCount,
        bool pendingOnly,
        CancellationToken cancellationToken)
    {
        var query = dbContext.OutboxMessages.AsQueryable();
        if (pendingOnly)
        {
            query = query.Where(x => x.ProcessedAt == null);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items
            .OrderBy(x => x.ProcessedAt is null ? 0 : 1)
            .ThenBy(x => x.OccurredAt)
            .Take(maxCount)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DepositOutboxMessage>> GetPendingOutboxMessagesAsync(int maxCount, CancellationToken cancellationToken)
    {
        var items = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .ToListAsync(cancellationToken);

        return items
            .OrderBy(x => x.OccurredAt)
            .Take(maxCount)
            .ToArray();
    }

    public async Task MarkOutboxMessageProcessedAsync(string messageId, DateTimeOffset processedAt, CancellationToken cancellationToken)
    {
        var message = await dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.ProcessedAt = processedAt;
        message.LastError = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RequeueOutboxMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        var message = await dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.ProcessedAt = null;
        message.LastError = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DepositTransaction transaction, CancellationToken cancellationToken)
    {
        dbContext.Deposits.Update(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
