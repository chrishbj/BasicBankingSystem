using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Auditing;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Exceptions;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositServiceTests
{
    private readonly InMemoryDepositRepository _repository;
    private readonly IDepositService _service;

    public DepositServiceTests()
    {
        _repository = new InMemoryDepositRepository();
        _service = new DepositService(
            _repository,
            new InMemoryDepositAccountDirectory(),
            new NoOpDepositTransactionProcessor(),
            new NullAuditLogWriter(),
            NullLogger<DepositService>.Instance);
    }

    [Fact]
    public async Task CreateDeposit_Should_Fail_When_AmountIsLessThanOrEqualToZero()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 0m, "CNY", DepositChannel.Counter, null, null);

        var act = () => _service.CreateAsync(request, "idem-001", "corr-001", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDepositRequestException>();
    }

    [Fact]
    public async Task CreateDeposit_Should_CreateReceivedTransaction_When_RequestIsValid()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 200m, "CNY", DepositChannel.Counter, null, null);

        var result = await _service.CreateAsync(request, "idem-002", "corr-002", CancellationToken.None);

        result.Status.Should().Be(DepositStatus.Received);
        result.PostedAt.Should().BeNull();

        var stored = await _repository.GetByIdAsync(result.TransactionId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(DepositStatus.Received);
        var pendingMessages = await _repository.GetPendingOutboxMessagesAsync(10, CancellationToken.None);
        pendingMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateDeposit_Should_ReturnExistingResult_When_IdempotencyKeyRepeated()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 300m, "CNY", DepositChannel.Counter, null, null);

        var first = await _service.CreateAsync(request, "idem-003", "corr-003", CancellationToken.None);
        var second = await _service.CreateAsync(request, "idem-003", "corr-004", CancellationToken.None);

        second.TransactionId.Should().Be(first.TransactionId);
        second.TransactionNumber.Should().Be(first.TransactionNumber);
    }

    [Fact]
    public async Task ResolvePendingReview_Should_CloseDepositAsReversed_When_ResolvedExternally()
    {
        var now = DateTimeOffset.UtcNow;
        var transaction = new DepositTransaction
        {
            TransactionId = "dep-review-001",
            TransactionNumber = "D202603280001",
            CustomerId = "cus_active_001",
            AccountId = "acc_active_001",
            Amount = 100m,
            Currency = "CNY",
            Channel = DepositChannel.Counter,
            Status = DepositStatus.PendingReview,
            AccountPostingStatus = DepositSagaStepStatus.Succeeded,
            AuditStatus = DepositSagaStepStatus.NotStarted,
            CompensationStatus = DepositSagaStepStatus.Failed,
            ReviewRequiredAt = now.AddMinutes(-5),
            IdempotencyKey = "idem-review-001",
            CorrelationId = "corr-review-001",
            RequestedAt = now.AddMinutes(-10)
        };

        await _repository.AddAsync(
            transaction,
            Banking.Services.Deposit.Messaging.DepositOutboxMessage.CreateRequestedMessage(
                new Banking.Services.Deposit.Messaging.DepositRequestedMessage(
                    transaction.TransactionId,
                    transaction.CustomerId,
                    transaction.AccountId,
                    transaction.Amount,
                    transaction.Currency,
                    transaction.Channel,
                    transaction.CorrelationId),
                transaction.RequestedAt),
            CancellationToken.None);

        var result = await _service.ResolvePendingReviewAsync(
            transaction.TransactionId,
            new ResolveDepositReviewRequest(DepositReviewResolution.ReversedExternally, "ops-user", "Reversed offline after ledger check."),
            CancellationToken.None);

        result.Status.Should().Be(DepositStatus.Reversed);
        result.ReviewResolution.Should().Be(DepositReviewResolution.ReversedExternally);
        result.ReviewLastActionBy.Should().Be("ops-user");
        result.ReviewResolvedAt.Should().NotBeNull();
    }

    private sealed class NoOpDepositTransactionProcessor : IDepositTransactionProcessor
    {
        public Task ProcessAsync(string transactionId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RetryCompensationAsync(string transactionId, string? requestedBy, string? note, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class NullAuditLogWriter : IAuditLogWriter
    {
        public Task WriteAsync(
            string action,
            DepositTransaction transaction,
            Dictionary<string, object?> beforeSnapshot,
            Dictionary<string, object?> afterSnapshot,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
