namespace Banking.Services.Account.CustomerDirectory;

public sealed record CustomerResponse(
    string CustomerId,
    CustomerDirectoryStatus Status);
