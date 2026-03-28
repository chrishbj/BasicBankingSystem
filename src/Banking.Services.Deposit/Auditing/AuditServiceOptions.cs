namespace Banking.Services.Deposit.Auditing;

public sealed class AuditServiceOptions
{
    public const string SectionName = "Infrastructure:AuditService";

    public string BaseUrl { get; init; } = "http://localhost:5104/";
    public int TimeoutSeconds { get; init; } = 10;
}
