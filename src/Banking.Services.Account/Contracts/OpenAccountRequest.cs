namespace Banking.Services.Account.Contracts;

public sealed record OpenAccountRequest(
    string CustomerId,
    string AccountType,
    string Currency);
