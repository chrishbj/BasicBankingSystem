namespace Banking.Services.Audit.Contracts;

public sealed record CreateAuditLogRequest(
    string ActorType,
    string ActorId,
    string Action,
    string AggregateType,
    string AggregateId,
    Dictionary<string, object?>? BeforeSnapshot,
    Dictionary<string, object?>? AfterSnapshot,
    string? CorrelationId,
    string? CausationId);
