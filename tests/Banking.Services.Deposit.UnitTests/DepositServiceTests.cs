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

    [Fact]
    public async Task GetPendingReview_Should_ReturnOnlyPendingReviewDeposits()
    {
        await SeedDepositAsync("dep-pending-001", DepositStatus.PendingReview);
        await SeedDepositAsync("dep-succeeded-001", DepositStatus.Succeeded);

        var result = await _service.GetPendingReviewAsync(1, 20, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Should().ContainSingle(item => item.TransactionId == "dep-pending-001");
    }

    [Fact]
    public async Task GetAll_Should_FilterByStatus_When_StatusProvided()
    {
        await SeedDepositAsync("dep-filter-pending-001", DepositStatus.PendingReview);
        await SeedDepositAsync("dep-filter-succeeded-001", DepositStatus.Succeeded);

        var result = await _service.GetAllAsync(
            new DepositSearchRequest(DepositStatus.PendingReview, null, null, null, null),
            1,
            20,
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Should().ContainSingle(item => item.TransactionId == "dep-filter-pending-001");
    }

    [Fact]
    public async Task GetAll_Should_FilterByCorrelationId_And_FailureCode_When_Provided()
    {
        var matchingRequestedAt = DateTimeOffset.UtcNow.AddMinutes(-15);
        await SeedDepositAsync("dep-filter-ops-001", DepositStatus.PendingReview, "corr-ops-001", "DEPOSIT_COMPENSATION_REVIEW_REQUIRED", matchingRequestedAt);
        await SeedDepositAsync("dep-filter-ops-002", DepositStatus.PendingReview, "corr-ops-002", "DEPOSIT_COMPENSATION_REVIEW_REQUIRED", matchingRequestedAt);
        await SeedDepositAsync("dep-filter-ops-003", DepositStatus.Failed, "corr-ops-001", "DEPOSIT_PROCESSING_FAILED", matchingRequestedAt);

        var result = await _service.GetAllAsync(
            new DepositSearchRequest(
                null,
                "corr-ops-001",
                "DEPOSIT_COMPENSATION_REVIEW_REQUIRED",
                matchingRequestedAt.AddMinutes(-1),
                matchingRequestedAt.AddMinutes(1)),
            1,
            20,
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.TransactionId == "dep-filter-ops-001");
    }

    private async Task SeedDepositAsync(
        string transactionId,
        DepositStatus status,
        string? correlationId = null,
        string? failureCode = null,
        DateTimeOffset? requestedAt = null)
    {
        var now = requestedAt ?? DateTimeOffset.UtcNow;
        await _repository.AddAsync(
            new DepositTransaction
            {
                TransactionId = transactionId,
                TransactionNumber = $"D{now:yyyyMMddHHmmssfff}{Random.Shared.Next(10, 99)}",
                CustomerId = "cus_active_001",
                AccountId = "acc_active_001",
                Amount = 100m,
                Currency = "CNY",
                Channel = DepositChannel.Counter,
                Status = status,
                AccountPostingStatus = status == DepositStatus.PendingReview ? DepositSagaStepStatus.Succeeded : DepositSagaStepStatus.Succeeded,
                AuditStatus = DepositSagaStepStatus.NotStarted,
                CompensationStatus = status == DepositStatus.PendingReview ? DepositSagaStepStatus.Failed : DepositSagaStepStatus.Skipped,
                ReviewRequiredAt = status == DepositStatus.PendingReview ? now.AddMinutes(-5) : null,
                IdempotencyKey = $"idem-{transactionId}",
                CorrelationId = correlationId ?? $"corr-{transactionId}",
                FailureCode = failureCode,
                RequestedAt = now
            },
            Banking.Services.Deposit.Messaging.DepositOutboxMessage.CreateRequestedMessage(
                new Banking.Services.Deposit.Messaging.DepositRequestedMessage(
                    transactionId,
                    "cus_active_001",
                    "acc_active_001",
                    100m,
                    "CNY",
                    DepositChannel.Counter,
                    correlationId ?? $"corr-{transactionId}"),
                now),
            CancellationToken.None);
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
