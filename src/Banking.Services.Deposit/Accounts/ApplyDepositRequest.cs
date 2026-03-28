namespace Banking.Services.Deposit.Accounts;

public sealed record ApplyDepositRequest(
    decimal Amount,
    string Currency);
