namespace Banking.Services.Account.Repositories;

public interface IAccountRepository
{
    Task AddAsync(Domain.Account account, CancellationToken cancellationToken);
    Task<Domain.Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken);
    Task<Domain.Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Domain.Account>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken);
    Task<Domain.AccountPosting?> GetPostingByReferenceAsync(string postingReference, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Domain.AccountPosting>> GetPostingsByAccountIdAsync(string accountId, CancellationToken cancellationToken);
    Task SavePostingAsync(Domain.Account account, Domain.AccountPosting posting, CancellationToken cancellationToken);
    Task UpdateAsync(Domain.Account account, CancellationToken cancellationToken);
}
