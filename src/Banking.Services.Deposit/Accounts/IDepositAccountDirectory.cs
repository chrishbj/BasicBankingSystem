namespace Banking.Services.Deposit.Accounts;

public interface IDepositAccountDirectory
{
    Task<DepositAccountRecord?> GetByIdAsync(string accountId, CancellationToken cancellationToken);
    Task PostDepositAsync(string accountId, decimal amount, string currency, CancellationToken cancellationToken);
}
