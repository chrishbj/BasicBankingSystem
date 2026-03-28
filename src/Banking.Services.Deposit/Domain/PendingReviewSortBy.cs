namespace Banking.Services.Deposit.Domain;

public enum PendingReviewSortBy
{
    ReviewRequiredAt = 1,
    LastCompensationAttemptAt = 2,
    RequestedAt = 3
}
