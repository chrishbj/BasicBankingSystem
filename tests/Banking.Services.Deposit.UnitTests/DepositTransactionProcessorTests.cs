using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Auditing;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Banking.Services.Deposit.UnitTests.Support;

namespace Banking.Services.Deposit.UnitTests;

public sealed class DepositTransactionProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Should_MarkDepositSucceeded_When_PostingSucceeds()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        var transaction = BuildTransaction("dep-success-001");

        repository.Setup(item => item.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        repository.Setup(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        accountDirectory
            .Setup(item => item.PostDepositAsync(transaction.AccountId, transaction.Amount, transaction.Currency, transaction.TransactionId, transaction.CorrelationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogWriter
            .Setup(item => item.WriteAsync("DepositSucceeded", transaction, It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processor = CreateProcessor(repository, accountDirectory, auditLogWriter);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        transaction.Status.Should().Be(DepositStatus.Succeeded);
        transaction.PostedAt.Should().NotBeNull();
        transaction.AuditStatus.Should().Be(DepositSagaStepStatus.Succeeded);
        repository.Verify(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>()), Times.Exactly(3));
        repository.VerifyAll();
        accountDirectory.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_Should_MarkDepositFailed_When_PostingThrows()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        var transaction = BuildTransaction("dep-failed-001");

        repository.Setup(item => item.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        repository.Setup(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        accountDirectory
            .Setup(item => item.PostDepositAsync(transaction.AccountId, transaction.Amount, transaction.Currency, transaction.TransactionId, transaction.CorrelationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated posting failure."));
        auditLogWriter
            .Setup(item => item.WriteAsync("DepositFailed", transaction, It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processor = CreateProcessor(repository, accountDirectory, auditLogWriter);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        transaction.Status.Should().Be(DepositStatus.Failed);
        transaction.FailureCode.Should().Be("DEPOSIT_PROCESSING_FAILED");
        transaction.AuditStatus.Should().Be(DepositSagaStepStatus.Succeeded);
        repository.Verify(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>()), Times.Exactly(3));
        repository.VerifyAll();
        accountDirectory.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_Should_NotRollbackTransaction_When_AuditRecordingFails()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        var transaction = BuildTransaction("dep-audit-failure-001");

        repository.Setup(item => item.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        repository.Setup(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        accountDirectory
            .Setup(item => item.PostDepositAsync(transaction.AccountId, transaction.Amount, transaction.Currency, transaction.TransactionId, transaction.CorrelationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogWriter
            .Setup(item => item.WriteAsync("DepositSucceeded", transaction, It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated audit failure."));

        var processor = CreateProcessor(repository, accountDirectory, auditLogWriter);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        transaction.Status.Should().Be(DepositStatus.Succeeded);
        transaction.AuditStatus.Should().Be(DepositSagaStepStatus.Failed);
        repository.Verify(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>()), Times.Exactly(3));
        repository.VerifyAll();
        accountDirectory.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_Should_Compensate_When_LocalFinalizationFails_After_AccountPosting()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        var transaction = BuildTransaction("dep-compensated-001");
        var shouldFailOnSucceededUpdate = true;

        repository.Setup(item => item.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        repository
            .Setup(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>()))
            .Returns<DepositTransaction, CancellationToken>((updated, _) =>
            {
                if (shouldFailOnSucceededUpdate && updated.Status == DepositStatus.Succeeded)
                {
                    shouldFailOnSucceededUpdate = false;
                    throw new InvalidOperationException("Simulated local finalization failure after account posting.");
                }

                return Task.CompletedTask;
            });
        accountDirectory
            .Setup(item => item.PostDepositAsync(transaction.AccountId, transaction.Amount, transaction.Currency, transaction.TransactionId, transaction.CorrelationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        accountDirectory
            .Setup(item => item.ReverseDepositAsync(transaction.AccountId, transaction.Amount, transaction.Currency, transaction.TransactionId, $"rev_{transaction.TransactionId}", transaction.CorrelationId, "Compensating partially completed deposit saga.", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogWriter
            .Setup(item => item.WriteAsync("DepositCompensated", transaction, It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processor = CreateProcessor(repository, accountDirectory, auditLogWriter);

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        transaction.Status.Should().Be(DepositStatus.Reversed);
        transaction.CompensationStatus.Should().Be(DepositSagaStepStatus.Compensated);
        transaction.AuditStatus.Should().Be(DepositSagaStepStatus.Succeeded);
        repository.VerifyAll();
        accountDirectory.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    [Fact]
    public async Task RetryCompensationAsync_Should_ResolvePendingReview_When_ReversalLaterSucceeds()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        var transaction = BuildTransaction("dep-review-retry-001");
        transaction.Status = DepositStatus.PendingReview;
        transaction.AccountPostingStatus = DepositSagaStepStatus.Succeeded;
        transaction.CompensationStatus = DepositSagaStepStatus.Failed;
        transaction.ReviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-2);

        repository.Setup(item => item.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        repository.Setup(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        accountDirectory
            .Setup(item => item.ReverseDepositAsync(transaction.AccountId, transaction.Amount, transaction.Currency, transaction.TransactionId, $"rev_{transaction.TransactionId}", transaction.CorrelationId, "Compensating partially completed deposit saga.", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogWriter
            .Setup(item => item.WriteAsync("DepositCompensated", transaction, It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processor = CreateProcessor(repository, accountDirectory, auditLogWriter);

        await processor.RetryCompensationAsync(transaction.TransactionId, "ops-user", "Retry after queue recovery.", CancellationToken.None);

        transaction.Status.Should().Be(DepositStatus.Reversed);
        transaction.CompensationStatus.Should().Be(DepositSagaStepStatus.Compensated);
        transaction.ReviewResolution.Should().Be(DepositReviewResolution.RetryRequested);
        transaction.ReviewLastActionBy.Should().Be("ops-user");
        transaction.CompensationRetryCount.Should().Be(1);
        transaction.AuditStatus.Should().Be(DepositSagaStepStatus.Succeeded);
        repository.VerifyAll();
        accountDirectory.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_Should_ReturnImmediately_When_TransactionAlreadySucceeded()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var transaction = DepositServiceTestData.CreateTransaction("dep-succeeded-001", DepositStatus.Succeeded);

        repository.Setup(item => item.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);

        var processor = CreateProcessor(repository, new Mock<IDepositAccountDirectory>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        await processor.ProcessAsync(transaction.TransactionId, CancellationToken.None);

        repository.Verify(item => item.UpdateAsync(It.IsAny<DepositTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RetryCompensationAsync_Should_MoveToPendingReview_When_ReversalFails()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        var accountDirectory = new Mock<IDepositAccountDirectory>(MockBehavior.Strict);
        var auditLogWriter = new Mock<IAuditLogWriter>(MockBehavior.Strict);
        var transaction = DepositServiceTestData.CreateTransaction("dep-review-failed-001", DepositStatus.PendingReview);
        transaction.AccountPostingStatus = DepositSagaStepStatus.Succeeded;
        transaction.CompensationStatus = DepositSagaStepStatus.Failed;
        transaction.FailureReason = "Initial failure";

        repository.Setup(item => item.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        repository.Setup(item => item.UpdateAsync(transaction, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        accountDirectory
            .Setup(item => item.ReverseDepositAsync(transaction.AccountId, transaction.Amount, transaction.Currency, transaction.TransactionId, $"rev_{transaction.TransactionId}", transaction.CorrelationId, "Compensating partially completed deposit saga.", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Reversal still failing."));
        auditLogWriter
            .Setup(item => item.WriteAsync("DepositCompensationPendingReview", transaction, It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processor = CreateProcessor(repository, accountDirectory, auditLogWriter);

        await processor.RetryCompensationAsync(transaction.TransactionId, "ops-user", "Retry note", CancellationToken.None);

        transaction.Status.Should().Be(DepositStatus.PendingReview);
        transaction.CompensationStatus.Should().Be(DepositSagaStepStatus.Failed);
        transaction.ReviewResolution.Should().Be(DepositReviewResolution.RetryRequested);
        transaction.FailureCode.Should().Be("DEPOSIT_COMPENSATION_REVIEW_REQUIRED");
        transaction.AuditStatus.Should().Be(DepositSagaStepStatus.Succeeded);
        repository.VerifyAll();
        accountDirectory.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    [Fact]
    public async Task RetryCompensationAsync_Should_Throw_When_TransactionDoesNotExist()
    {
        var repository = new Mock<IDepositRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("dep-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepositTransaction?)null);

        var processor = CreateProcessor(repository, new Mock<IDepositAccountDirectory>(MockBehavior.Strict), new Mock<IAuditLogWriter>(MockBehavior.Strict));

        var act = () => processor.RetryCompensationAsync("dep-missing", "ops-user", "Retry note", CancellationToken.None);

        await act.Should().ThrowAsync<Banking.Services.Deposit.Exceptions.DepositNotFoundException>();
        repository.VerifyAll();
    }

    private static DepositTransactionProcessor CreateProcessor(
        Mock<IDepositRepository> repository,
        Mock<IDepositAccountDirectory> accountDirectory,
        Mock<IAuditLogWriter> auditLogWriter)
        => new(
            repository.Object,
            accountDirectory.Object,
            auditLogWriter.Object,
            NullLogger<DepositTransactionProcessor>.Instance);

    private static DepositTransaction BuildTransaction(string transactionId)
        => DepositServiceTestData.CreateTransaction(transactionId, DepositStatus.Received, correlationId: $"corr-{transactionId}");
}
