using System.Collections.Concurrent;

namespace Banking.Services.Account.CustomerDirectory;

public sealed class InMemoryCustomerDirectory : ICustomerDirectory
{
    private readonly ConcurrentDictionary<string, CustomerDirectoryRecord> _customers;

    public InMemoryCustomerDirectory()
    {
        _customers = new ConcurrentDictionary<string, CustomerDirectoryRecord>(
            new[]
            {
                new KeyValuePair<string, CustomerDirectoryRecord>(
                    "cus_active_001",
                    new CustomerDirectoryRecord("cus_active_001", CustomerDirectoryStatus.Active)),
                new KeyValuePair<string, CustomerDirectoryRecord>(
                    "cus_frozen_001",
                    new CustomerDirectoryRecord("cus_frozen_001", CustomerDirectoryStatus.Frozen))
            });
    }

    public Task<CustomerDirectoryRecord?> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        _customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }
}
