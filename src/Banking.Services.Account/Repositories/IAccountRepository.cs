namespace Banking.Services.Account.Repositories;

public interface IAccountRepository
{
    Task AddAsync(Domain.Account account, CancellationToken cancellationToken);
    Task<Domain.Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Domain.Account>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken);
    Task UpdateAsync(Domain.Account account, CancellationToken cancellationToken);
}
