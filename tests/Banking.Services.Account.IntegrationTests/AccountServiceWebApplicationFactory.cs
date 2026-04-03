using Banking.Testing.Shared;

namespace Banking.Services.Account.IntegrationTests;

public sealed class AccountServiceWebApplicationFactory : SqliteWebApplicationFactory<Program>
{
    public AccountServiceWebApplicationFactory()
        : base("basicbanking-account-tests")
    {
    }
}
