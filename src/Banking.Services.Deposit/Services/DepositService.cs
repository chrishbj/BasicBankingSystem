using Banking.BuildingBlocks.Contracts;
using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Auditing;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Exceptions;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;

namespace Banking.Services.Deposit.Services;

public sealed class DepositService(
    IDepositRepository depositRepository,
    IDepositAccountDirectory accountDirectory,
    IDepositTransactionProcessor depositTransactionProcessor,
    IAuditLogWriter auditLogWriter,
    ILogger<DepositService> logger) : IDepositService
{
    public async Task<DepositResponse> CreateAsync(
        CreateDepositRequest request,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidDepositRequestException("Deposit amount must be greater than zero.");
        }

        var existing = await depositRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            // Idempotency protects the balance from duplicate client retries.
            return Map(existing);
        }

        var account = await accountDirectory.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new InvalidDepositRequestException("Account was not found.");
        }

        if (!string.Equals(account.CustomerId, request.CustomerId, StringComparison.Ordinal))
        {
            throw new InvalidDepositRequestException("Customer and account do not match.");
        }

        if (account.Status != DepositAccountStatus.Active)
        {
            throw new InvalidDepositRequestException($"Account status is '{account.Status}'.");
        }

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDepositRequestException("Currency does not match account currency.");
        }

        var now = DateTimeOffset.UtcNow;
        var referenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
            ? null
            : request.ReferenceNumber.Trim();
        var transaction = new DepositTransaction
        {
            TransactionId = $"dep_{Guid.NewGuid():N}",
            TransactionNumber = $"D{now:yyyyMMddHHmmssfff}{Random.Shared.Next(10, 99)}",
            CustomerId = request.CustomerId,
            AccountId = request.AccountId,
            Amount = request.Amount,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            ReferenceNumber = referenceNumber,
            Channel = request.Channel,
            Status = DepositStatus.Received,
            AccountPostingStatus = DepositSagaStepStatus.NotStarted,
            AuditStatus = DepositSagaStepStatus.NotStarted,
            CompensationStatus = DepositSagaStepStatus.NotStarted,
            ReviewResolution = DepositReviewResolution.None,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId,
            RequestedAt = now
        };

        // Outbox pattern: persist the workflow state and the integration message together
        // so message publication can be retried safely by a background dispatcher.
        var requestedMessage = new DepositRequestedMessage(
            transaction.TransactionId,
            transaction.CustomerId,
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.Channel,
            transaction.CorrelationId);

        await depositRepository.AddAsync(
            transaction,
            DepositOutboxMessage.CreateRequestedMessage(requestedMessage, now),
            cancellationToken);

        return Map(transaction);
    }

    public async Task<DepositResponse> CreatePendingReviewDemoAsync(
        CreatePendingReviewDemoRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidDepositRequestException("Demo deposit amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerId) || string.IsNullOrWhiteSpace(request.AccountId))
        {
            throw new InvalidDepositRequestException("CustomerId and AccountId are required.");
        }

        var account = await accountDirectory.GetByIdAsync(request.AccountId.Trim(), cancellationToken);
        if (account is null)
        {
            throw new InvalidDepositRequestException("Account was not found.");
        }

        if (!string.Equals(account.CustomerId, request.CustomerId.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidDepositRequestException("Customer and account do not match.");
        }

        if (account.Status != DepositAccountStatus.Active)
        {
            throw new InvalidDepositRequestException($"Account status is '{account.Status}'.");
        }

        var now = DateTimeOffset.UtcNow;
        var transaction = new DepositTransaction
        {
            TransactionId = $"dep_{Guid.NewGuid():N}",
            TransactionNumber = $"D{now:yyyyMMddHHmmssfff}{Random.Shared.Next(10, 99)}",
            CustomerId = request.CustomerId.Trim(),
            AccountId = request.AccountId.Trim(),
            Amount = request.Amount,
            Currency = account.Currency,
            ReferenceNumber = $"DEMO-REV-{now:yyyyMMddHHmmssfff}",
            Channel = DepositChannel.Counter,
            Status = DepositStatus.PendingReview,
            AccountPostingStatus = DepositSagaStepStatus.Succeeded,
            AuditStatus = DepositSagaStepStatus.NotStarted,
            CompensationStatus = DepositSagaStepStatus.Failed,
            ReviewResolution = DepositReviewResolution.None,
            IdempotencyKey = $"demo-review-{Guid.NewGuid():N}",
            CorrelationId = correlationId,
            FailureCode = "DEPOSIT_COMPENSATION_REVIEW_REQUIRED",
            FailureReason = string.IsNullOrWhiteSpace(request.Note)
                ? "Demo pending-review item created from the local operations console."
                : request.Note.Trim(),
            // Keep demo items in PendingReview so the UI can exercise the human recovery path
            // without the automatic retry worker immediately consuming them.
            CompensationRetryCount = 3,
            RequestedAt = now,
            PostedAt = now,
            ReviewRequiredAt = now,
            LastCompensationAttemptAt = now,
            LastProcessedAt = now
        };

        await accountDirectory.PostDepositAsync(
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.TransactionId,
            transaction.CorrelationId,
            cancellationToken);

        await depositRepository.AddAsync(
            transaction,
            new DepositOutboxMessage
            {
                MessageId = $"out_{Guid.NewGuid():N}",
                TransactionId = transaction.TransactionId,
                MessageType = "PendingReviewDemoCreated",
                Payload = "{}",
                OccurredAt = now,
                ProcessedAt = now
            },
            cancellationToken);

        await TryRecordReviewAuditAsync(transaction, cancellationToken);
        return Map(transaction);
    }

    public async Task<DepositResponse> GetByIdAsync(string transactionId, CancellationToken cancellationToken)
    {
        var transaction = await depositRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new DepositNotFoundException(transactionId);

        return Map(transaction);
    }

    public async Task<PagedResponse<DepositSummaryResponse>> GetAllAsync(
        DepositSearchRequest request,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var deposits = await depositRepository.GetAllAsync(cancellationToken);

        if (request.Status is not null)
        {
            deposits = deposits.Where(item => item.Status == request.Status.Value).ToArray();
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            deposits = deposits
                .Where(item => string.Equals(item.CustomerId, request.CustomerId.Trim(), StringComparison.Ordinal))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(request.AccountId))
        {
            deposits = deposits
                .Where(item => string.Equals(item.AccountId, request.AccountId.Trim(), StringComparison.Ordinal))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            deposits = deposits
                .Where(item => string.Equals(item.CorrelationId, request.CorrelationId.Trim(), StringComparison.Ordinal))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(request.FailureCode))
        {
            deposits = deposits
                .Where(item => string.Equals(item.FailureCode, request.FailureCode.Trim(), StringComparison.Ordinal))
                .ToArray();
        }

        if (request.RequestedFrom is not null)
        {
            deposits = deposits
                .Where(item => item.RequestedAt >= request.RequestedFrom.Value)
                .ToArray();
        }

        if (request.RequestedTo is not null)
        {
            deposits = deposits
                .Where(item => item.RequestedAt <= request.RequestedTo.Value)
                .ToArray();
        }

        var totalCount = deposits.Count;
        var items = deposits
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new DepositSummaryResponse(
                item.TransactionId,
                item.TransactionNumber,
                item.CustomerId,
                item.AccountId,
                item.Amount,
                item.Currency,
                item.ReferenceNumber,
                item.Channel,
                item.Status,
                item.RequestedAt,
                item.PostedAt))
            .ToArray();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<DepositSummaryResponse>(items, pageNumber, pageSize, totalCount, totalPages);
    }

    public async Task<PagedResponse<PendingReviewDepositSummaryResponse>> GetPendingReviewAsync(
        PendingReviewSortBy sortBy,
        bool descending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // PendingReview is the explicit "automation stopped here" queue for operators.
        var deposits = await depositRepository.GetPendingReviewAsync(int.MaxValue, cancellationToken);
        deposits = ApplyPendingReviewSort(deposits, sortBy, descending);
        var totalCount = deposits.Count;
        var items = deposits
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new PendingReviewDepositSummaryResponse(
                item.TransactionId,
                item.TransactionNumber,
                item.CustomerId,
                item.AccountId,
                item.Amount,
                item.Currency,
                item.CompensationStatus,
                item.ReviewResolution,
                item.FailureCode,
                item.FailureReason,
                item.CompensationRetryCount,
                item.ReviewLastActionBy,
                item.ReviewNote,
                item.RequestedAt,
                item.ReviewRequiredAt,
                item.LastCompensationAttemptAt,
                item.LastProcessedAt))
            .ToArray();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<PendingReviewDepositSummaryResponse>(items, pageNumber, pageSize, totalCount, totalPages);
    }

    private static IReadOnlyCollection<DepositTransaction> ApplyPendingReviewSort(
        IReadOnlyCollection<DepositTransaction> deposits,
        PendingReviewSortBy sortBy,
        bool descending)
    {
        Func<DepositTransaction, DateTimeOffset> keySelector = sortBy switch
        {
            PendingReviewSortBy.LastCompensationAttemptAt => item => item.LastCompensationAttemptAt ?? DateTimeOffset.MinValue,
            PendingReviewSortBy.RequestedAt => item => item.RequestedAt,
            _ => item => item.ReviewRequiredAt ?? item.RequestedAt
        };

        return descending
            ? deposits.OrderByDescending(keySelector).ToArray()
            : deposits.OrderBy(keySelector).ToArray();
    }

    public async Task<DepositResponse> RetryPendingReviewAsync(
        string transactionId,
        RetryDepositReviewRequest request,
        CancellationToken cancellationToken)
    {
        // Retry routes back into the compensation branch of the saga instead of creating
        // a separate recovery workflow.
        await depositTransactionProcessor.RetryCompensationAsync(
            transactionId,
            request.OperatorId,
            request.Note,
            cancellationToken);

        return await GetByIdAsync(transactionId, cancellationToken);
    }

    public async Task<DepositResponse> ResolvePendingReviewAsync(
        string transactionId,
        ResolveDepositReviewRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await depositRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new DepositNotFoundException(transactionId);

        if (transaction.Status != DepositStatus.PendingReview)
        {
            throw new InvalidDepositReviewActionException(transactionId, "Only pending review deposits can be resolved.");
        }

        if (string.IsNullOrWhiteSpace(request.OperatorId) || string.IsNullOrWhiteSpace(request.Note))
        {
            throw new InvalidDepositReviewActionException(transactionId, "OperatorId and note are required.");
        }

        switch (request.Resolution)
        {
            case DepositReviewResolution.ReversedExternally:
                transaction.Status = DepositStatus.Reversed;
                transaction.CompensationStatus = DepositSagaStepStatus.Compensated;
                transaction.ReversedAt = DateTimeOffset.UtcNow;
                transaction.ReviewResolution = DepositReviewResolution.ReversedExternally;
                transaction.FailureCode = "DEPOSIT_COMPENSATED_EXTERNALLY";
                transaction.FailureReason = request.Note;
                break;
            case DepositReviewResolution.FailedExternally:
                transaction.Status = DepositStatus.Failed;
                transaction.ReviewResolution = DepositReviewResolution.FailedExternally;
                transaction.FailureCode = "DEPOSIT_REVIEW_RESOLVED_EXTERNALLY";
                transaction.FailureReason = request.Note;
                break;
            default:
                throw new InvalidDepositReviewActionException(
                    transactionId,
                    $"Resolution '{request.Resolution}' is not supported for manual review closure.");
        }

        transaction.ReviewLastActionBy = request.OperatorId.Trim();
        transaction.ReviewNote = request.Note.Trim();
        transaction.ReviewResolvedAt = DateTimeOffset.UtcNow;
        transaction.LastProcessedAt = DateTimeOffset.UtcNow;
        await depositRepository.UpdateAsync(transaction, cancellationToken);
        await TryRecordReviewAuditAsync(transaction, cancellationToken);

        return Map(transaction);
    }

    private async Task TryRecordReviewAuditAsync(
        DepositTransaction transaction,
        CancellationToken cancellationToken)
    {
        var snapshot = BuildSnapshot(transaction);

        try
        {
            await auditLogWriter.WriteAsync(
                "DepositReviewResolved",
                transaction,
                snapshot,
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
            await depositRepository.UpdateAsync(transaction, cancellationToken);
            logger.LogError(
                exception,
                "Audit recording failed for manual deposit review resolution {TransactionId}.",
                transaction.TransactionId);
        }
    }

    private static Dictionary<string, object?> BuildSnapshot(DepositTransaction transaction)
    {
        return new Dictionary<string, object?>
        {
            ["transactionId"] = transaction.TransactionId,
            ["status"] = transaction.Status.ToString(),
            ["referenceNumber"] = transaction.ReferenceNumber,
            ["failureCode"] = transaction.FailureCode,
            ["failureReason"] = transaction.FailureReason,
            ["accountPostingStatus"] = transaction.AccountPostingStatus.ToString(),
            ["auditStatus"] = transaction.AuditStatus.ToString(),
            ["compensationStatus"] = transaction.CompensationStatus.ToString(),
            ["reviewResolution"] = transaction.ReviewResolution.ToString(),
            ["compensationRetryCount"] = transaction.CompensationRetryCount,
            ["reviewLastActionBy"] = transaction.ReviewLastActionBy,
            ["reviewNote"] = transaction.ReviewNote,
            ["reviewRequiredAt"] = transaction.ReviewRequiredAt,
            ["reviewResolvedAt"] = transaction.ReviewResolvedAt,
            ["lastCompensationAttemptAt"] = transaction.LastCompensationAttemptAt,
            ["lastProcessedAt"] = transaction.LastProcessedAt
        };
    }

    private static DepositResponse Map(DepositTransaction transaction)
    {
        return new DepositResponse(
            transaction.TransactionId,
            transaction.TransactionNumber,
            transaction.CustomerId,
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.ReferenceNumber,
            transaction.Channel,
            transaction.Status,
            transaction.AccountPostingStatus,
            transaction.AuditStatus,
            transaction.CompensationStatus,
            transaction.ReviewResolution,
            transaction.CorrelationId,
            transaction.FailureCode,
            transaction.FailureReason,
            transaction.CompensationRetryCount,
            transaction.ReviewLastActionBy,
            transaction.ReviewNote,
            transaction.RequestedAt,
            transaction.PostedAt,
            transaction.ReversedAt,
            transaction.ReviewRequiredAt,
            transaction.ReviewResolvedAt,
            transaction.LastCompensationAttemptAt,
            transaction.LastProcessedAt);
    }
}
