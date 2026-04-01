namespace Banking.BuildingBlocks.Security;

public sealed class BankingSecurityOptions
{
    public const string SectionName = "Security";

    public BankingAuthenticationSettings Authentication { get; set; } = new();

    public CurrentServiceIdentityOptions CurrentServiceIdentity { get; set; } = new();
}

public sealed class BankingAuthenticationSettings
{
    public bool Enabled { get; set; } = true;

    public List<ExternalApiKeyOptions> ExternalApiKeys { get; set; } = [];

    public List<InternalServiceKeyOptions> InternalServices { get; set; } = [];
}

public sealed class ExternalApiKeyOptions
{
    public string Name { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string PrincipalType { get; set; } = BankingPrincipalTypes.ExternalClient;

    public List<string> Roles { get; set; } = [];
}

public sealed class InternalServiceKeyOptions
{
    public string Name { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string PrincipalType { get; set; } = BankingPrincipalTypes.InternalService;

    public List<string> Roles { get; set; } = [];
}

public sealed class CurrentServiceIdentityOptions
{
    public string ServiceName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
