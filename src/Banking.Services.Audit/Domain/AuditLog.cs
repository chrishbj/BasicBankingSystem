namespace Banking.Services.Audit.Domain;

public sealed class AuditLog
{
    public string AuditId { get; init; } = default!;
    public string ActorType { get; init; } = default!;
    public string ActorId { get; init; } = default!;
    public string Action { get; init; } = default!;
    public string AggregateType { get; init; } = default!;
    public string AggregateId { get; init; } = default!;
    public Dictionary<string, object?>? BeforeSnapshot { get; init; }
    public Dictionary<string, object?>? AfterSnapshot { get; init; }
    public string CorrelationId { get; init; } = default!;
    public string? CausationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
