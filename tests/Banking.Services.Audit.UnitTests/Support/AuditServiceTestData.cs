using Banking.Services.Audit.Domain;

namespace Banking.Services.Audit.UnitTests.Support;

internal static class AuditServiceTestData
{
    public static AuditLog CreateAuditLog(string auditId, string action, DateTimeOffset occurredAt)
        => new()
        {
            AuditId = auditId,
            ActorType = "User",
            ActorId = "user_001",
            Action = action,
            AggregateType = "Customer",
            AggregateId = "cus_001",
            CorrelationId = "corr-001",
            OccurredAt = occurredAt
        };
}
