using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Repositories;

namespace Banking.Services.Deposit.Services;

public sealed class DepositTransactionProcessor(
    IDepositRepository depositRepository,
    IDepositAccountDirectory accountDirectory) : IDepositTransactionProcessor
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
            transaction.Status = DepositStatus.Processing;
            await depositRepository.UpdateAsync(transaction, cancellationToken);

            await accountDirectory.PostDepositAsync(transaction.AccountId, transaction.Amount, cancellationToken);

            transaction.Status = DepositStatus.Succeeded;
            transaction.PostedAt = DateTimeOffset.UtcNow;
            transaction.FailureCode = null;
            transaction.FailureReason = null;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
        }
        catch (Exception exception)
        {
            transaction.Status = DepositStatus.Failed;
            transaction.FailureCode = "DEPOSIT_PROCESSING_FAILED";
            transaction.FailureReason = exception.Message;
            await depositRepository.UpdateAsync(transaction, cancellationToken);
        }
    }
}
