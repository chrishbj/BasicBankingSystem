namespace Banking.Services.Deposit.Contracts;

public sealed record CreatePendingReviewDemoRequest(
    string CustomerId,
    string AccountId,
    decimal Amount,
    string? Note);
