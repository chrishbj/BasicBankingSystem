namespace Banking.Services.Deposit.Services;

public interface IDepositTransactionProcessor
{
    Task ProcessAsync(string transactionId, CancellationToken cancellationToken);
    Task RetryCompensationAsync(string transactionId, string? requestedBy, string? note, CancellationToken cancellationToken);
}
