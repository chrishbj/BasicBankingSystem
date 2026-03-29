using System.Collections.Concurrent;

namespace Banking.Services.Customer.Repositories;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<string, Domain.Customer> _customers = new();

    public Task<bool> ExistsByIdentityAsync(string identityType, string identityNumber, CancellationToken cancellationToken)
    {
        var exists = _customers.Values.Any(customer =>
            string.Equals(customer.IdentityType, identityType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(customer.IdentityNumber, identityNumber, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByMobileAsync(string mobile, string? excludingCustomerId, CancellationToken cancellationToken)
    {
        var exists = _customers.Values.Any(customer =>
            !string.Equals(customer.CustomerId, excludingCustomerId, StringComparison.Ordinal) &&
            string.Equals(customer.Mobile, mobile, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public Task AddAsync(Domain.Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.CustomerId] = customer;
        return Task.CompletedTask;
    }

    public Task<Domain.Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        _customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }

    public Task<Domain.Customer?> GetByCustomerNumberAsync(string customerNumber, CancellationToken cancellationToken)
    {
        var customer = _customers.Values.FirstOrDefault(item =>
            string.Equals(item.CustomerNumber, customerNumber, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(customer);
    }

    public Task<IReadOnlyCollection<Domain.Customer>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Domain.Customer>>(
            _customers.Values
                .OrderByDescending(customer => customer.CreatedAt)
                .ToArray());
    }

    public Task UpdateAsync(Domain.Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.CustomerId] = customer;
        return Task.CompletedTask;
    }
}
