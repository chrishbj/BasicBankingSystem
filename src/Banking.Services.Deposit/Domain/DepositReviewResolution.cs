namespace Banking.Services.Deposit.Domain;

public enum DepositReviewResolution
{
    None = 1,
    RetryRequested = 2,
    ReversedExternally = 3,
    FailedExternally = 4
}
