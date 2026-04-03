using Banking.Testing.Shared;

namespace Banking.Services.Audit.IntegrationTests;

public sealed class AuditServiceWebApplicationFactory : SqliteWebApplicationFactory<Program>
{
    public AuditServiceWebApplicationFactory()
        : base("basicbanking-audit-tests")
    {
    }
}
