using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Auditing;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Repositories;

namespace Banking.Services.Deposit.Services;

public sealed class DepositTransactionProcessor(
    IDepositRepository depositRepository,
    IDepositAccountDirectory accountDirectory,
    IAuditLogWriter auditLogWriter,
    ILogger<DepositTransactionProcessor> logger) : IDepositTransactionProcessor
{
    public async Task ProcessAsync(string transactionId, CancellationToken cancellationToken)
    {
        var transaction = await depositRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null || transaction.Status is DepositStatus.Succeeded or DepositStatus.Reversed)
        {
            return;
        }

        var accountPostingCompleted = false;

        try
        {
            var postingReference = string.IsNullOrWhiteSpace(transaction.ReferenceNumber)
                ? transaction.TransactionId
                : transaction.ReferenceNumber.Trim();

            transaction.Status = DepositStatus.Processing;
            transaction.AccountPostingStatus = DepositSagaStepStatus.InProgress;
            transaction.CompensationStatus = DepositSagaStepStatus.NotStarted;
            transaction.ReviewResolution = DepositReviewResolution.None;
            transaction.ReviewRequiredAt = null;
            transaction.ReviewResolvedAt = null;
            transaction.ReviewLastActionBy = null;
            transaction.ReviewNote = null;
            transaction.LastProcessedAt = DateTimeOffset.UtcNow;
            await depositRepository.UpdateAsync(transaction, cancellationToken);

            await accountDirectory.PostDepositAsync(
                transaction.AccountId,
                transaction.Amount,
                transaction.Currency,
                postingReference,
                transaction.CorrelationId,
                cancellationToken);

            accountPostingCompleted = true;
            transaction.Status = DepositStatus.Succeeded;
            transaction.AccountPostingStatus = DepositSagaStepStatus.Succeeded;
            transaction.CompensationStatus = DepositSagaStepStatus.Skipped;
            transaction.ReviewResolution = DepositReviewResolution.None;
            transaction.PostedAt = DateTimeOffset.UtcNow;
            transaction.FailureCode = null;
            transaction.FailureReason = null;
            transaction.LastProcessedAt = DateTimeOffset.UtcNow;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
            await TryRecordAuditAsync("DepositSucceeded", transaction, cancellationToken);
        }
        catch (Exception exception)
        {
            if (accountPostingCompleted)
            {
                await CompensateAsync(transaction, exception, null, null, cancellationToken);
                return;
            }

            await FailProcessingAsync(transaction, exception.Message, cancellationToken);
        }
    }

    public async Task RetryCompensationAsync(
        string transactionId,
        string? requestedBy,
        string? note,
        CancellationToken cancellationToken)
    {
        var transaction = await depositRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new Exceptions.DepositNotFoundException(transactionId);

        if (transaction.Status != DepositStatus.PendingReview)
        {
            throw new Exceptions.InvalidDepositReviewActionException(
                transactionId,
                "Only pending review deposits can retry compensation.");
        }

        await CompensateAsync(
            transaction,
            new InvalidOperationException(transaction.FailureReason ?? "Retry requested for pending review deposit."),
            requestedBy,
            note,
            cancellationToken);
    }

    private async Task FailProcessingAsync(
        DepositTransaction transaction,
        string failureReason,
        CancellationToken cancellationToken)
    {
        transaction.Status = DepositStatus.Failed;
        transaction.AccountPostingStatus = DepositSagaStepStatus.Failed;
        transaction.CompensationStatus = DepositSagaStepStatus.NotStarted;
        transaction.ReviewResolution = DepositReviewResolution.None;
        transaction.FailureCode = "DEPOSIT_PROCESSING_FAILED";
        transaction.FailureReason = failureReason;
        transaction.LastProcessedAt = DateTimeOffset.UtcNow;
        await depositRepository.UpdateAsync(transaction, cancellationToken);
        await TryRecordAuditAsync("DepositFailed", transaction, cancellationToken);
    }

    private async Task CompensateAsync(
        DepositTransaction transaction,
        Exception processingException,
        string? requestedBy,
        string? note,
        CancellationToken cancellationToken)
    {
        var postingReference = string.IsNullOrWhiteSpace(transaction.ReferenceNumber)
            ? transaction.TransactionId
            : transaction.ReferenceNumber.Trim();

        transaction.CompensationRetryCount++;
        transaction.LastCompensationAttemptAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(requestedBy))
        {
            transaction.ReviewLastActionBy = requestedBy.Trim();
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            transaction.ReviewNote = note.Trim();
        }
        
        try
        {
            transaction.CompensationStatus = DepositSagaStepStatus.InProgress;
            transaction.ReviewResolution = requestedBy is null
                ? DepositReviewResolution.None
                : DepositReviewResolution.RetryRequested;
            transaction.LastProcessedAt = DateTimeOffset.UtcNow;
            await depositRepository.UpdateAsync(transaction, cancellationToken);

            await accountDirectory.ReverseDepositAsync(
                transaction.AccountId,
                transaction.Amount,
                transaction.Currency,
                postingReference,
                $"rev_{transaction.TransactionId}",
                transaction.CorrelationId,
                "Compensating partially completed deposit saga.",
                cancellationToken);

            transaction.Status = DepositStatus.Reversed;
            transaction.CompensationStatus = DepositSagaStepStatus.Compensated;
            transaction.ReviewRequiredAt = null;
            transaction.ReviewResolvedAt = DateTimeOffset.UtcNow;
            transaction.FailureCode = "DEPOSIT_COMPENSATED";
            transaction.FailureReason = processingException.Message;
            transaction.ReversedAt = DateTimeOffset.UtcNow;
            transaction.LastProcessedAt = DateTimeOffset.UtcNow;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
            await TryRecordAuditAsync("DepositCompensated", transaction, cancellationToken);
        }
        catch (Exception compensationException)
        {
            transaction.Status = DepositStatus.PendingReview;
            transaction.CompensationStatus = DepositSagaStepStatus.Failed;
            transaction.ReviewRequiredAt ??= DateTimeOffset.UtcNow;
            transaction.FailureCode = "DEPOSIT_COMPENSATION_REVIEW_REQUIRED";
            transaction.FailureReason = compensationException.Message;
            transaction.LastProcessedAt = DateTimeOffset.UtcNow;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
            await TryRecordAuditAsync("DepositCompensationPendingReview", transaction, cancellationToken);
        }
    }

    private async Task TryRecordAuditAsync(
        string action,
        DepositTransaction transaction,
        CancellationToken cancellationToken)
    {
        var beforeSnapshot = BuildSnapshot(transaction);

        try
        {
            await auditLogWriter.WriteAsync(
                action,
                transaction,
                beforeSnapshot,
                BuildSnapshot(transaction),
                cancellationToken);
            transaction.AuditStatus = DepositSagaStepStatus.Succeeded;
            transaction.LastProcessedAt = DateTimeOffset.UtcNow;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
        }
        catch (Exception exception)
        {
            transaction.AuditStatus = DepositSagaStepStatus.Failed;
            transaction.LastProcessedAt = DateTimeOffset.UtcNow;
            try
            {
                await depositRepository.UpdateAsync(transaction, cancellationToken);
            }
            catch (Exception persistenceException)
            {
                logger.LogError(
                    persistenceException,
                    "Persisting audit failure state failed for deposit transaction {TransactionId}.",
                    transaction.TransactionId);
            }

            logger.LogError(
                exception,
                "Audit recording failed for deposit transaction {TransactionId} and action {Action}.",
                transaction.TransactionId,
                action);
        }
    }

    private static Dictionary<string, object?> BuildSnapshot(DepositTransaction transaction)
    {
        return new Dictionary<string, object?>
        {
            ["transactionId"] = transaction.TransactionId,
            ["transactionNumber"] = transaction.TransactionNumber,
            ["customerId"] = transaction.CustomerId,
            ["accountId"] = transaction.AccountId,
            ["amount"] = transaction.Amount,
            ["currency"] = transaction.Currency,
            ["referenceNumber"] = transaction.ReferenceNumber,
            ["status"] = transaction.Status.ToString(),
            ["failureCode"] = transaction.FailureCode,
            ["failureReason"] = transaction.FailureReason,
            ["accountPostingStatus"] = transaction.AccountPostingStatus.ToString(),
            ["auditStatus"] = transaction.AuditStatus.ToString(),
            ["compensationStatus"] = transaction.CompensationStatus.ToString(),
            ["reviewResolution"] = transaction.ReviewResolution.ToString(),
            ["requestedAt"] = transaction.RequestedAt,
            ["postedAt"] = transaction.PostedAt,
            ["reversedAt"] = transaction.ReversedAt,
            ["compensationRetryCount"] = transaction.CompensationRetryCount,
            ["reviewLastActionBy"] = transaction.ReviewLastActionBy,
            ["reviewNote"] = transaction.ReviewNote,
            ["reviewRequiredAt"] = transaction.ReviewRequiredAt,
            ["reviewResolvedAt"] = transaction.ReviewResolvedAt,
            ["lastCompensationAttemptAt"] = transaction.LastCompensationAttemptAt,
            ["lastProcessedAt"] = transaction.LastProcessedAt
        };
    }
}
