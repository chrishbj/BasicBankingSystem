namespace Banking.Services.Deposit.Services;

public interface IDepositTransactionProcessor
{
    Task ProcessAsync(string transactionId, CancellationToken cancellationToken);
}
