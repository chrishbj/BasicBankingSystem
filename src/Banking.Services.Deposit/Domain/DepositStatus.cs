namespace Banking.Services.Deposit.Domain;

public enum DepositStatus
{
    Received = 1,
    Processing = 2,
    Succeeded = 3,
    Rejected = 4,
    Failed = 5,
    PendingReview = 6
}
