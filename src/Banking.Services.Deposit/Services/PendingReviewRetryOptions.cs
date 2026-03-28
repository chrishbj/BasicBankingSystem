namespace Banking.Services.Deposit.Services;

public sealed class PendingReviewRetryOptions
{
    public const string SectionName = "Deposit:PendingReview";

    public bool Enabled { get; init; } = true;
    public int MaxAutomaticRetries { get; init; } = 3;
    public int PollingIntervalMilliseconds { get; init; } = 2000;
}
