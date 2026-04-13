using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Auditing;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Exceptions;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Banking.Services.Deposit.UnitTests.Support;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositServiceTests
{
    [Fact]
    public async Task CreateDeposit_Should_Fail_When_AmountIsLessThanOrEqualToZero()
    {
        var service = CreateService(
            new Mock<IDepositRepository>(MockBehavior.Strict),
            new Mock<IDepositAccountDirectory>(MockBehavior.Strict),
            new Mock<IDepositTransactionProcessor>(MockBehavior.Strict),
            new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 0m, "USD", DepositChannel.Counter, null, null);

        var act = () => service.CreateAsync(request, "idem-001", "corr-001", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDepositRequestException>();
    }

    [Fact]
    public async Task CreateDeposit_Should_CreateReceivedTransaction_When_RequestIsValid()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        DepositTransaction? savedTransaction = null;
        DepositOutboxMessage? savedOutbox = null;

        repository
            .Setup(item => item.GetByIdempotencyKeyAsync("idem-002", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepositTransaction?)null);
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccountRecord());
        repository
            .Setup(item => item.AddAsync(It.IsAny<DepositTransaction>(), It.IsAny<DepositOutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<DepositTransaction, DepositOutboxMessage, CancellationToken>((transaction, outbox, _) =>
            {
                savedTransaction = transaction;
                savedOutbox = outbox;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 200m, "usd", DepositChannel.Counter, " ref-001 ", null);

        var result = await service.CreateAsync(request, "idem-002", "corr-002", CancellationToken.None);

        result.Status.Should().Be(DepositStatus.Received);
        result.PostedAt.Should().BeNull();
        savedTransaction.Should().NotBeNull();
        savedTransaction!.Currency.Should().Be("USD");
        savedTransaction.ReferenceNumber.Should().Be("ref-001");
        savedOutbox.Should().NotBeNull();
        savedOutbox!.TransactionId.Should().Be(savedTransaction.TransactionId);
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task CreateDeposit_Should_Fail_When_AccountCustomerDoesNotMatch()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);

        repository
            .Setup(item => item.GetByIdempotencyKeyAsync("idem-mismatch", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepositTransaction?)null);
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DepositServiceTestData.CreateAccountRecord(customerId: "cus_other_001"));

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var act = () => service.CreateAsync(
            new CreateDepositRequest("cus_active_001", "acc_active_001", 200m, "USD", DepositChannel.Counter, null, null),
            "idem-mismatch",
            "corr-mismatch",
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDepositRequestException>().WithMessage("*Customer and account do not match.*");
        repository.Verify(item => item.AddAsync(It.IsAny<DepositTransaction>(), It.IsAny<DepositOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task CreateDeposit_Should_Fail_When_AccountDoesNotExist()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);

        repository
            .Setup(item => item.GetByIdempotencyKeyAsync("idem-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepositTransaction?)null);
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_missing_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepositAccountRecord?)null);

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var act = () => service.CreateAsync(
            new CreateDepositRequest("cus_active_001", "acc_missing_001", 200m, "USD", DepositChannel.Counter, null, null),
            "idem-missing",
            "corr-missing",
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDepositRequestException>().WithMessage("*Account was not found*");
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task CreateDeposit_Should_ReturnExistingResult_When_IdempotencyKeyRepeated()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var existing = CreateTransaction("dep-existing-001", DepositStatus.Received);

        repository
            .Setup(item => item.GetByIdempotencyKeyAsync("idem-003", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var service = CreateService(repository, new Mock<IDepositAccountDirectory>(MockBehavior.Strict), new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 300m, "USD", DepositChannel.Counter, null, null);

        var result = await service.CreateAsync(request, "idem-003", "corr-003", CancellationToken.None);

        result.TransactionId.Should().Be("dep-existing-001");
        repository.Verify(item => item.AddAsync(It.IsAny<DepositTransaction>(), It.IsAny<DepositOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ResolvePendingReview_Should_CloseDepositAsReversed_When_ResolvedExternally()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        var transaction = CreateTransaction("dep-review-001", DepositStatus.PendingReview);
        transaction.AccountPostingStatus = DepositSagaStepStatus.Succeeded;
        transaction.CompensationStatus = DepositSagaStepStatus.Failed;
        transaction.ReviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        repository
            .Setup(item => item.GetByIdAsync("dep-review-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        repository
            .Setup(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogWriter
            .Setup(item => item.WriteAsync(
                "DepositReviewResolved",
                transaction,
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository, new Mock<IDepositAccountDirectory>(MockBehavior.Strict), new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), auditLogWriter);

        var result = await service.ResolvePendingReviewAsync(
            transaction.TransactionId,
            new ResolveDepositReviewRequest(DepositReviewResolution.ReversedExternally, "ops-user", "Reversed offline after ledger check."),
            CancellationToken.None);

        result.Status.Should().Be(DepositStatus.Reversed);
        result.ReviewResolution.Should().Be(DepositReviewResolution.ReversedExternally);
        result.ReviewLastActionBy.Should().Be("ops-user");
        result.ReviewResolvedAt.Should().NotBeNull();
        repository.Verify(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>()), Times.Exactly(2));
        auditLogWriter.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreatePendingReviewDemo_Should_CreatePendingReviewDeposit_And_RecordAudit()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        DepositTransaction? savedTransaction = null;

        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DepositServiceTestData.CreateAccountRecord());
        accountDirectory
            .Setup(item => item.PostDepositAsync("acc_active_001", 200m, "USD", It.IsAny<string>(), "corr-demo", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(item => item.AddAsync(It.IsAny<DepositTransaction>(), It.IsAny<DepositOutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<DepositTransaction, DepositOutboxMessage, CancellationToken>((transaction, _, _) => savedTransaction = transaction)
            .Returns(Task.CompletedTask);
        repository
            .Setup(item => item.UpdateAsync(It.IsAny<DepositTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogWriter
            .Setup(item => item.WriteAsync("DepositReviewResolved", It.IsAny<DepositTransaction>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), auditLogWriter);

        var result = await service.CreatePendingReviewDemoAsync(
            new CreatePendingReviewDemoRequest(" cus_active_001 ", " acc_active_001 ", 200m, " demo note "),
            "corr-demo",
            CancellationToken.None);

        result.Status.Should().Be(DepositStatus.PendingReview);
        result.FailureCode.Should().Be("DEPOSIT_COMPENSATION_REVIEW_REQUIRED");
        savedTransaction.Should().NotBeNull();
        savedTransaction!.CustomerId.Should().Be("cus_active_001");
        savedTransaction.AccountId.Should().Be("acc_active_001");
        savedTransaction.AuditStatus.Should().Be(DepositSagaStepStatus.Succeeded);
        repository.VerifyAll();
        accountDirectory.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    [Fact]
    public async Task GetById_Should_Throw_When_TransactionDoesNotExist()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("dep_missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepositTransaction?)null);

        var service = CreateService(repository, new Mock<IDepositAccountDirectory>(MockBehavior.Strict), new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var act = () => service.GetByIdAsync("dep_missing", CancellationToken.None);

        await act.Should().ThrowAsync<DepositNotFoundException>();
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetPendingReview_Should_ReturnOnlyPendingReviewDeposits()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var pending = CreateTransaction("dep-pending-001", DepositStatus.PendingReview, accountId: "acc_active_001");

        repository
            .Setup(item => item.GetPendingReviewAsync(int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { pending });
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccountRecord());

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var result = await service.GetPendingReviewAsync(
            PendingReviewSortBy.ReviewRequiredAt,
            false,
            1,
            20,
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.TransactionId == "dep-pending-001");
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task GetPendingReview_Should_SortByLastCompensationAttemptDescending_When_Requested()
    {
        var first = CreateTransaction("dep-pending-sort-001", DepositStatus.PendingReview, requestedAt: DateTimeOffset.UtcNow.AddMinutes(-20));
        first.LastCompensationAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        first.ReviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-15);

        var second = CreateTransaction("dep-pending-sort-002", DepositStatus.PendingReview, requestedAt: DateTimeOffset.UtcNow.AddMinutes(-25));
        second.LastCompensationAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        second.ReviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-20);

        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetPendingReviewAsync(int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { first, second });
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccountRecord());

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var result = await service.GetPendingReviewAsync(
            PendingReviewSortBy.LastCompensationAttemptAt,
            true,
            1,
            20,
            CancellationToken.None);

        result.Items.Select(item => item.TransactionId).Should().ContainInOrder("dep-pending-sort-002", "dep-pending-sort-001");
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task GetAll_Should_FilterByStatus_When_StatusProvided()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateTransaction("dep-filter-pending-001", DepositStatus.PendingReview),
                CreateTransaction("dep-filter-succeeded-001", DepositStatus.Succeeded, accountId: "acc_other_001")
            });
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccountRecord());

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var result = await service.GetAllAsync(
            new DepositSearchRequest(DepositStatus.PendingReview, null, null, null, null, null, null),
            1,
            20,
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.TransactionId == "dep-filter-pending-001");
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task GetAll_Should_FilterByCorrelationId_And_FailureCode_When_Provided()
    {
        var matchingRequestedAt = DateTimeOffset.UtcNow.AddMinutes(-15);
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateTransaction("dep-filter-ops-001", DepositStatus.PendingReview, correlationId: "corr-ops-001", failureCode: "DEPOSIT_COMPENSATION_REVIEW_REQUIRED", requestedAt: matchingRequestedAt),
                CreateTransaction("dep-filter-ops-002", DepositStatus.PendingReview, correlationId: "corr-ops-002", failureCode: "DEPOSIT_COMPENSATION_REVIEW_REQUIRED", requestedAt: matchingRequestedAt),
                CreateTransaction("dep-filter-ops-003", DepositStatus.Failed, correlationId: "corr-ops-001", failureCode: "DEPOSIT_PROCESSING_FAILED", requestedAt: matchingRequestedAt)
            });
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccountRecord());

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var result = await service.GetAllAsync(
            new DepositSearchRequest(null, null, null, "corr-ops-001", "DEPOSIT_COMPENSATION_REVIEW_REQUIRED", matchingRequestedAt.AddMinutes(-1), matchingRequestedAt.AddMinutes(1)),
            1,
            20,
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.TransactionId == "dep-filter-ops-001");
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task GetAll_Should_FilterByCustomerId_And_AccountId_When_Provided()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateTransaction("dep-filter-account-001", DepositStatus.Succeeded, customerId: "cus_active_001", accountId: "acc_active_001"),
                CreateTransaction("dep-filter-account-002", DepositStatus.Succeeded, customerId: "cus_active_001", accountId: "acc_other_001"),
                CreateTransaction("dep-filter-account-003", DepositStatus.Succeeded, customerId: "cus_other_001", accountId: "acc_active_001")
            });
        accountDirectory
            .Setup(item => item.GetByIdAsync("acc_active_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccountRecord());

        var service = CreateService(repository, accountDirectory, new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var result = await service.GetAllAsync(
            new DepositSearchRequest(DepositStatus.Succeeded, "cus_active_001", "acc_active_001", null, null, null, null),
            1,
            20,
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.TransactionId == "dep-filter-account-001");
        repository.VerifyAll();
        accountDirectory.VerifyAll();
    }

    [Fact]
    public async Task RetryPendingReview_Should_DelegateToProcessor_AndReturnReloadedTransaction()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var processor = new Mock<IDepositTransactionProcessor>(MockBehavior.Strict);
        var transaction = DepositServiceTestData.CreateTransaction("dep-retry-001", DepositStatus.PendingReview);

        processor
            .Setup(item => item.RetryCompensationAsync("dep-retry-001", "ops-user", "Retry note", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(item => item.GetByIdAsync("dep-retry-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var service = CreateService(repository, new Mock<IDepositAccountDirectory>(MockBehavior.Strict), processor, new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var result = await service.RetryPendingReviewAsync(
            "dep-retry-001",
            new RetryDepositReviewRequest("ops-user", "Retry note"),
            CancellationToken.None);

        result.TransactionId.Should().Be("dep-retry-001");
        processor.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task ResolvePendingReview_Should_Fail_When_DepositIsNotPendingReview()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var transaction = DepositServiceTestData.CreateTransaction("dep-not-pending", DepositStatus.Succeeded);
        repository
            .Setup(item => item.GetByIdAsync("dep-not-pending", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var service = CreateService(repository, new Mock<IDepositAccountDirectory>(MockBehavior.Strict), new Mock<IDepositTransactionProcessor>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var act = () => service.ResolvePendingReviewAsync(
            "dep-not-pending",
            new ResolveDepositReviewRequest(DepositReviewResolution.ReversedExternally, "ops-user", "note"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDepositReviewActionException>()
            .WithMessage("*Only pending review deposits can be resolved*");
        repository.VerifyAll();
    }

    private static DepositService CreateService(
        Mock<IDepositRepository> repository,
        Mock<IDepositAccountDirectory> accountDirectory,
        Mock<IDepositTransactionProcessor> processor,
        Mock<IAuditLogWriter> auditLogWriter)
        => new(
            repository.Object,
            accountDirectory.Object,
            processor.Object,
            auditLogWriter.Object,
            Options.Create(new PendingReviewRetryOptions()),
            Options.Create(new RabbitMqOptions()),
            NullLogger<DepositService>.Instance);

    private static DepositAccountRecord CreateAccountRecord(string accountId = "acc_active_001")
        => DepositServiceTestData.CreateAccountRecord(accountId);

    private static DepositTransaction CreateTransaction(
        string transactionId,
        DepositStatus status,
        string customerId = "cus_active_001",
        string accountId = "acc_active_001",
        string correlationId = "corr-default",
        string? failureCode = null,
        DateTimeOffset? requestedAt = null)
        => DepositServiceTestData.CreateTransaction(transactionId, status, customerId, accountId, correlationId, failureCode, requestedAt);
}
