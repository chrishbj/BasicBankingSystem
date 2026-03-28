namespace Banking.Services.Customer.Repositories;

public interface ICustomerRepository
{
    Task<bool> ExistsByIdentityAsync(string identityType, string identityNumber, CancellationToken cancellationToken);
    Task<bool> ExistsByMobileAsync(string mobile, string? excludingCustomerId, CancellationToken cancellationToken);
    Task AddAsync(Domain.Customer customer, CancellationToken cancellationToken);
    Task<Domain.Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Domain.Customer>> GetAllAsync(CancellationToken cancellationToken);
    Task UpdateAsync(Domain.Customer customer, CancellationToken cancellationToken);
}
