using Banking.Services.Customer.Data;
using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Customer.Repositories;

public sealed class EfCustomerRepository(CustomerDbContext dbContext) : ICustomerRepository
{
    public Task<bool> ExistsByIdentityAsync(string identityType, string identityNumber, CancellationToken cancellationToken)
    {
        return dbContext.Customers.AnyAsync(
            customer => customer.IdentityType == identityType && customer.IdentityNumber == identityNumber,
            cancellationToken);
    }

    public Task<bool> ExistsByMobileAsync(string mobile, string? excludingCustomerId, CancellationToken cancellationToken)
    {
        return dbContext.Customers.AnyAsync(
            customer => customer.Mobile == mobile && customer.CustomerId != excludingCustomerId,
            cancellationToken);
    }

    public async Task AddAsync(Domain.Customer customer, CancellationToken cancellationToken)
    {
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Domain.Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        return dbContext.Customers.FirstOrDefaultAsync(customer => customer.CustomerId == customerId, cancellationToken);
    }

    public Task<Domain.Customer?> GetByCustomerNumberAsync(string customerNumber, CancellationToken cancellationToken)
    {
        return dbContext.Customers.FirstOrDefaultAsync(
            customer => customer.CustomerNumber == customerNumber,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Customer>> GetAllAsync(CancellationToken cancellationToken)
    {
        var customers = await dbContext.Customers.ToListAsync(cancellationToken);
        return customers
            .OrderByDescending(customer => customer.CreatedAt)
            .ToArray();
    }

    public async Task UpdateAsync(Domain.Customer customer, CancellationToken cancellationToken)
    {
        dbContext.Customers.Update(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
