using Banking.Testing.Shared;

namespace Banking.Services.Customer.IntegrationTests;

public sealed class CustomerServiceWebApplicationFactory : SqliteWebApplicationFactory<Program>
{
    public CustomerServiceWebApplicationFactory()
        : base("basicbanking-customer-tests")
    {
    }
}
