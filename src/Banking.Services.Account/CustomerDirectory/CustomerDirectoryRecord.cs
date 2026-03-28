namespace Banking.Services.Account.CustomerDirectory;

public sealed record CustomerDirectoryRecord(
    string CustomerId,
    CustomerDirectoryStatus Status);

public enum CustomerDirectoryStatus
{
    Pending = 1,
    Active = 2,
    Frozen = 3,
    Closed = 4
}
