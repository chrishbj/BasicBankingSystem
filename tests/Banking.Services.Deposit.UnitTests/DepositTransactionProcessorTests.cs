using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
using FluentAssertions;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositTransactionProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Should_MarkDepositSucceeded_When_PostingSucceeds()
    {
        var repository = new InMemoryDepositRepository();
        var transaction = BuildTransaction("dep-success-001");
        await repository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(BuildRequestedMessage(transaction), transaction.RequestedAt),
            CancellationToken.None);

        var processor = new DepositTransactionProcessor(repository, new InMemoryDepositAccountDirectory());

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        var stored = await repository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Succeeded);
        stored.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsync_Should_MarkDepositFailed_When_PostingThrows()
    {
        var repository = new InMemoryDepositRepository();
        var transaction = BuildTransaction("dep-failed-001");
        await repository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(BuildRequestedMessage(transaction), transaction.RequestedAt),
            CancellationToken.None);

        var processor = new DepositTransactionProcessor(repository, new ThrowingDepositAccountDirectory());

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        var stored = await repository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Failed);
        stored.FailureCode.Should().Be("DEPOSIT_PROCESSING_FAILED");
        stored.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    private static DepositTransaction BuildTransaction(string transactionId)
    {
        return new DepositTransaction
        {
            TransactionId = transactionId,
            TransactionNumber = $"D{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
            CustomerId = "cus_active_001",
            AccountId = "acc_active_001",
            Amount = 100m,
            Currency = "CNY",
            Channel = DepositChannel.Counter,
            Status = DepositStatus.Received,
            IdempotencyKey = $"idem-{transactionId}",
            CorrelationId = $"corr-{transactionId}",
            RequestedAt = DateTimeOffset.UtcNow
        };
    }

    private static DepositRequestedMessage BuildRequestedMessage(DepositTransaction transaction)
    {
        return new DepositRequestedMessage(
            transaction.TransactionId,
            transaction.CustomerId,
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.Channel,
            transaction.CorrelationId);
    }

    private sealed class ThrowingDepositAccountDirectory : IDepositAccountDirectory
    {
        public Task<DepositAccountRecord?> GetByIdAsync(string accountId, CancellationToken cancellationToken)
        {
            return Task.FromResult<DepositAccountRecord?>(null);
        }

        public Task PostDepositAsync(string accountId, decimal amount, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated posting failure.");
        }
    }
}
