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
}
