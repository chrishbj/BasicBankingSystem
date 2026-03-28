namespace Banking.Services.Deposit.Contracts;

public sealed record RetryDepositReviewRequest(
    string? OperatorId,
    string? Note);
