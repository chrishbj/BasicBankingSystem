namespace Banking.Services.Audit.Contracts;

public sealed record AuditLogResponse(
    string AuditId,
    string ActorType,
    string ActorId,
    string Action,
    string AggregateType,
    string AggregateId,
    Dictionary<string, object?>? BeforeSnapshot,
    Dictionary<string, object?>? AfterSnapshot,
    string CorrelationId,
    string? CausationId,
    DateTimeOffset OccurredAt);

public sealed record AuditSummaryResponse(
    string AuditId,
    string ActorType,
    string ActorId,
    string Action,
    string AggregateType,
    string AggregateId,
    string CorrelationId,
    DateTimeOffset OccurredAt);
