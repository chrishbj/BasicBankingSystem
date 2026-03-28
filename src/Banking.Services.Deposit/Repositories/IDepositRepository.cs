using Banking.Services.Deposit.Domain;

namespace Banking.Services.Deposit.Repositories;

public interface IDepositRepository
{
    Task AddAsync(DepositTransaction transaction, CancellationToken cancellationToken);
    Task<DepositTransaction?> GetByIdAsync(string transactionId, CancellationToken cancellationToken);
    Task<DepositTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DepositTransaction>> GetAllAsync(CancellationToken cancellationToken);
    Task UpdateAsync(DepositTransaction transaction, CancellationToken cancellationToken);
}
