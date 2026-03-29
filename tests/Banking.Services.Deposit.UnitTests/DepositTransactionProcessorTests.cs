using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Auditing;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositTransactionProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Should_MarkDepositSucceeded_When_PostingSucceeds()
    {
        // Happy-path saga test: posting succeeds, audit succeeds, and the workflow reaches Succeeded.
        var repository = new InMemoryDepositRepository();
        var transaction = BuildTransaction("dep-success-001");
        await repository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(BuildRequestedMessage(transaction), transaction.RequestedAt),
            CancellationToken.None);

        var auditLogWriter = new TestAuditLogWriter();
        var processor = new DepositTransactionProcessor(
            repository,
            new InMemoryDepositAccountDirectory(),
            auditLogWriter,
            NullLogger<DepositTransactionProcessor>.Instance);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        var stored = await repository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Succeeded);
        stored.PostedAt.Should().NotBeNull();
        auditLogWriter.Actions.Should().ContainSingle().Which.Should().Be("DepositSucceeded");
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

        var auditLogWriter = new TestAuditLogWriter();
        var processor = new DepositTransactionProcessor(
            repository,
            new ThrowingDepositAccountDirectory(),
            auditLogWriter,
            NullLogger<DepositTransactionProcessor>.Instance);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        var stored = await repository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Failed);
        stored.FailureCode.Should().Be("DEPOSIT_PROCESSING_FAILED");
        stored.FailureReason.Should().NotBeNullOrWhiteSpace();
        auditLogWriter.Actions.Should().ContainSingle().Which.Should().Be("DepositFailed");
    }

    [Fact]
    public async Task ProcessAsync_Should_NotRollbackTransaction_When_AuditRecordingFails()
    {
        // Audit failure is intentionally non-transactional with respect to the balance workflow.
        var repository = new InMemoryDepositRepository();
        var transaction = BuildTransaction("dep-audit-failure-001");
        await repository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(BuildRequestedMessage(transaction), transaction.RequestedAt),
            CancellationToken.None);

        var processor = new DepositTransactionProcessor(
            repository,
            new InMemoryDepositAccountDirectory(),
            new ThrowingAuditLogWriter(),
            NullLogger<DepositTransactionProcessor>.Instance);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        var stored = await repository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Succeeded);
    }

    [Fact]
    public async Task ProcessAsync_Should_Compensate_When_LocalFinalizationFails_After_AccountPosting()
    {
        // This simulates the most interesting distributed failure: the balance moved, but a later
        // local step failed, so the saga must compensate instead of simply returning Failed.
        var repository = new FailAfterPostingDepositRepository();
        var transaction = BuildTransaction("dep-compensated-001");
        await repository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(BuildRequestedMessage(transaction), transaction.RequestedAt),
            CancellationToken.None);

        var accountDirectory = new TrackingDepositAccountDirectory();
        var auditLogWriter = new TestAuditLogWriter();
        var processor = new DepositTransactionProcessor(
            repository,
            accountDirectory,
            auditLogWriter,
            NullLogger<DepositTransactionProcessor>.Instance);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        var stored = await repository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Reversed);
        stored.CompensationStatus.Should().Be(DepositSagaStepStatus.Compensated);
        accountDirectory.PostedReferences.Should().Contain(transaction.TransactionId);
        accountDirectory.ReversedReferences.Should().Contain($"rev_{transaction.TransactionId}");
        auditLogWriter.Actions.Should().Contain("DepositCompensated");
    }

    [Fact]
    public async Task RetryCompensationAsync_Should_ResolvePendingReview_When_ReversalLaterSucceeds()
    {
        // Review retry proves the workflow can re-enter the compensation branch after an operator action.
        var repository = new InMemoryDepositRepository();
        var transaction = BuildTransaction("dep-review-retry-001");
        transaction.Status = DepositStatus.PendingReview;
        transaction.AccountPostingStatus = DepositSagaStepStatus.Succeeded;
        transaction.CompensationStatus = DepositSagaStepStatus.Failed;
        transaction.ReviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        await repository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(BuildRequestedMessage(transaction), transaction.RequestedAt),
            CancellationToken.None);

        var accountDirectory = new RetryableCompensationDepositAccountDirectory();
        var auditLogWriter = new TestAuditLogWriter();
        var processor = new DepositTransactionProcessor(
            repository,
            accountDirectory,
            auditLogWriter,
            NullLogger<DepositTransactionProcessor>.Instance);

        await processor.RetryCompensationAsync(transaction.TransactionId, "ops-user", "Retry after queue recovery.", CancellationToken.None);

        var stored = await repository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Reversed);
        stored.CompensationStatus.Should().Be(DepositSagaStepStatus.Compensated);
        stored.ReviewResolution.Should().Be(DepositReviewResolution.RetryRequested);
        stored.ReviewLastActionBy.Should().Be("ops-user");
        stored.CompensationRetryCount.Should().Be(1);
        auditLogWriter.Actions.Should().Contain("DepositCompensated");
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
            Currency = "USD",
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

        public Task PostDepositAsync(
            string accountId,
            decimal amount,
            string currency,
            string postingReference,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated posting failure.");
        }

        public Task ReverseDepositAsync(
            string accountId,
            decimal amount,
            string currency,
            string originalPostingReference,
            string reversalReference,
            string? correlationId,
            string reason,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingDepositAccountDirectory : IDepositAccountDirectory
    {
        public List<string> PostedReferences { get; } = [];
        public List<string> ReversedReferences { get; } = [];

        public Task<DepositAccountRecord?> GetByIdAsync(string accountId, CancellationToken cancellationToken)
        {
            return Task.FromResult<DepositAccountRecord?>(null);
        }

        public Task PostDepositAsync(
            string accountId,
            decimal amount,
            string currency,
            string postingReference,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            PostedReferences.Add(postingReference);
            return Task.CompletedTask;
        }

        public Task ReverseDepositAsync(
            string accountId,
            decimal amount,
            string currency,
            string originalPostingReference,
            string reversalReference,
            string? correlationId,
            string reason,
            CancellationToken cancellationToken)
        {
            ReversedReferences.Add(reversalReference);
            return Task.CompletedTask;
        }
    }

    private sealed class RetryableCompensationDepositAccountDirectory : IDepositAccountDirectory
    {
        public Task<DepositAccountRecord?> GetByIdAsync(string accountId, CancellationToken cancellationToken)
        {
            return Task.FromResult<DepositAccountRecord?>(null);
        }

        public Task PostDepositAsync(
            string accountId,
            decimal amount,
            string currency,
            string postingReference,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ReverseDepositAsync(
            string accountId,
            decimal amount,
            string currency,
            string originalPostingReference,
            string reversalReference,
            string? correlationId,
            string reason,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FailAfterPostingDepositRepository : InMemoryDepositRepository
    {
        private bool _shouldFailOnSucceededUpdate = true;

        public override Task UpdateAsync(DepositTransaction transaction, CancellationToken cancellationToken)
        {
            if (_shouldFailOnSucceededUpdate && transaction.Status == DepositStatus.Succeeded)
            {
                _shouldFailOnSucceededUpdate = false;
                throw new InvalidOperationException("Simulated local finalization failure after account posting.");
            }

            return base.UpdateAsync(transaction, cancellationToken);
        }
    }

    private sealed class TestAuditLogWriter : IAuditLogWriter
    {
        public List<string> Actions { get; } = new();

        public Task WriteAsync(
            string action,
            DepositTransaction transaction,
            Dictionary<string, object?> beforeSnapshot,
            Dictionary<string, object?> afterSnapshot,
            CancellationToken cancellationToken)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAuditLogWriter : IAuditLogWriter
    {
        public Task WriteAsync(
            string action,
            DepositTransaction transaction,
            Dictionary<string, object?> beforeSnapshot,
            Dictionary<string, object?> afterSnapshot,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated audit failure.");
        }
    }
}
