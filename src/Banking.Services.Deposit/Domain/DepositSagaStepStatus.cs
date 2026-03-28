namespace Banking.Services.Deposit.Domain;

public enum DepositSagaStepStatus
{
    NotStarted = 1,
    InProgress = 2,
    Succeeded = 3,
    Failed = 4,
    Compensated = 5,
    Skipped = 6
}
