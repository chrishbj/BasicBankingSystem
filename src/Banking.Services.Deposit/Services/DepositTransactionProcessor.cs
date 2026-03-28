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
        if (transaction is null || transaction.Status == DepositStatus.Succeeded)
        {
            return;
        }

        try
        {
            var beforeSnapshot = BuildSnapshot(transaction);
            transaction.Status = DepositStatus.Processing;
            await depositRepository.UpdateAsync(transaction, cancellationToken);

            await accountDirectory.PostDepositAsync(
                transaction.AccountId,
                transaction.Amount,
                transaction.Currency,
                cancellationToken);

            transaction.Status = DepositStatus.Succeeded;
            transaction.PostedAt = DateTimeOffset.UtcNow;
            transaction.FailureCode = null;
            transaction.FailureReason = null;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
            await RecordAuditAsync("DepositSucceeded", transaction, beforeSnapshot, cancellationToken);
        }
        catch (Exception exception)
        {
            var beforeSnapshot = BuildSnapshot(transaction);
            transaction.Status = DepositStatus.Failed;
            transaction.FailureCode = "DEPOSIT_PROCESSING_FAILED";
            transaction.FailureReason = exception.Message;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
            await RecordAuditAsync("DepositFailed", transaction, beforeSnapshot, cancellationToken);
        }
    }

    private async Task RecordAuditAsync(
        string action,
        DepositTransaction transaction,
        Dictionary<string, object?> beforeSnapshot,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditLogWriter.WriteAsync(
                action,
                transaction,
                beforeSnapshot,
                BuildSnapshot(transaction),
                cancellationToken);
        }
        catch (Exception exception)
        {
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
            ["status"] = transaction.Status.ToString(),
            ["failureCode"] = transaction.FailureCode,
            ["failureReason"] = transaction.FailureReason,
            ["requestedAt"] = transaction.RequestedAt,
            ["postedAt"] = transaction.PostedAt
        };
    }
}
