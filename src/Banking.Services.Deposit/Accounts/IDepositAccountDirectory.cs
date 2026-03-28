namespace Banking.Services.Deposit.Accounts;

public interface IDepositAccountDirectory
{
    Task<DepositAccountRecord?> GetByIdAsync(string accountId, CancellationToken cancellationToken);
    Task PostDepositAsync(
        string accountId,
        decimal amount,
        string currency,
        string postingReference,
        string? correlationId,
        CancellationToken cancellationToken);
    Task ReverseDepositAsync(
        string accountId,
        decimal amount,
        string currency,
        string originalPostingReference,
        string reversalReference,
        string? correlationId,
        string reason,
        CancellationToken cancellationToken);
}
