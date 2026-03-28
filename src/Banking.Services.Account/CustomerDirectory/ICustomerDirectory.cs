namespace Banking.Services.Account.CustomerDirectory;

public interface ICustomerDirectory
{
    Task<CustomerDirectoryRecord?> GetByIdAsync(string customerId, CancellationToken cancellationToken);
}
