namespace Banking.Bff.CustomerPortal.Options;

public sealed class DownstreamServiceOptions
{
    public const string SectionName = "Infrastructure";

    public string CustomerServiceBaseUrl { get; set; } = "http://localhost:18081/";

    public string AccountServiceBaseUrl { get; set; } = "http://localhost:18082/";

    public string DepositServiceBaseUrl { get; set; } = "http://localhost:18083/";

    public string ExternalApiKey { get; set; } = "local-dev-api-key";
}
