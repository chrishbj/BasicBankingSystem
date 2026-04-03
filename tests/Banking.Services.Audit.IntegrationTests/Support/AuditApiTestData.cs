using Banking.Services.Audit.Contracts;

namespace Banking.Services.Audit.IntegrationTests.Support;

internal static class AuditApiTestData
{
    public static CreateAuditLogRequest CreateAudit(string action = "DepositSucceeded")
        => new(
            "User",
            "user_001",
            action,
            "DepositTransaction",
            $"dep_{Guid.NewGuid():N}",
            new Dictionary<string, object?> { ["status"] = "Processing" },
            new Dictionary<string, object?> { ["status"] = "Succeeded" },
            $"corr-{Guid.NewGuid():N}",
            $"cmd-{Guid.NewGuid():N}");
}
