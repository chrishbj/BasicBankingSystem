namespace Banking.Services.Deposit.Accounts;

public sealed class AccountServiceOptions
{
    public const string SectionName = "Infrastructure:AccountService";

    public string BaseUrl { get; init; } = "http://localhost:5102/";
    public int TimeoutSeconds { get; init; } = 10;
}
