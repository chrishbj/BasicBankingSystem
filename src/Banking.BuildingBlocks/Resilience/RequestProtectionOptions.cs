namespace Banking.BuildingBlocks.Resilience;

public sealed class RequestProtectionOptions
{
    public const string SectionName = "RequestProtection";

    public bool EnableGlobalRateLimiting { get; set; } = true;
    public int GlobalPermitLimit { get; set; } = 120;
    public int GlobalWindowSeconds { get; set; } = 10;
    public int GlobalQueueLimit { get; set; } = 0;
    public bool EnableIdempotencyReplayBackoff { get; set; } = true;
    public int ReplayTrackingWindowSeconds { get; set; } = 30;
    public int ReplayCacheLifetimeSeconds { get; set; } = 60;
    public int ImmediateReplayLimit { get; set; } = 1;
    public int ReplayBaseDelayMilliseconds { get; set; } = 200;
    public int ReplayMaxDelayMilliseconds { get; set; } = 1000;
}
