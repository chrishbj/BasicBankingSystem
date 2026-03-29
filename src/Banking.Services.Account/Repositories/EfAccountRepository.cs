using Banking.Services.Account.Data;
using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Account.Repositories;

public sealed class EfAccountRepository(AccountDbContext dbContext) : IAccountRepository
{
    public async Task AddAsync(Domain.Account account, CancellationToken cancellationToken)
    {
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Domain.Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken)
    {
        return dbContext.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Account>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken)
    {
        var items = await dbContext.Accounts
            .Where(x => x.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.OpenedAt)
            .ToArray();
    }

    public Task<Domain.AccountPosting?> GetPostingByReferenceAsync(string postingReference, CancellationToken cancellationToken)
    {
        return dbContext.AccountPostings.FirstOrDefaultAsync(
            x => x.PostingReference == postingReference,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.AccountPosting>> GetPostingsByAccountIdAsync(string accountId, CancellationToken cancellationToken)
    {
        var items = await dbContext.AccountPostings
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.CreatedAt)
            .ToArray();
    }

    public async Task SavePostingAsync(Domain.Account account, Domain.AccountPosting posting, CancellationToken cancellationToken)
    {
        dbContext.Accounts.Update(account);
        dbContext.AccountPostings.Add(posting);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Domain.Account account, CancellationToken cancellationToken)
    {
        dbContext.Accounts.Update(account);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
