namespace Banking.Services.Account.CustomerDirectory;

public sealed class CustomerServiceOptions
{
    public const string SectionName = "Infrastructure:CustomerService";

    public string BaseUrl { get; set; } = "http://localhost:5101/";

    public int TimeoutSeconds { get; set; } = 10;
}
