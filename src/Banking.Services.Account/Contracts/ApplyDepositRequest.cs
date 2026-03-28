namespace Banking.Services.Account.Contracts;

public sealed record ApplyDepositRequest(
    decimal Amount,
    string Currency);
