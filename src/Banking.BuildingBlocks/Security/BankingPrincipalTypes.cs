namespace Banking.BuildingBlocks.Security;

public static class BankingPrincipalTypes
{
    public const string ExternalClient = "external-client";
    public const string Customer = "customer";
    public const string BusinessUser = "business-user";
    public const string PlatformOperator = "platform-operator";
    public const string PlatformAdministrator = "platform-administrator";
    public const string SecurityAdministrator = "security-administrator";
    public const string VendorEngineer = "vendor-engineer";
    public const string TestAccount = "test-account";

    public const string InternalService = "internal-service";

    public const string PrincipalTypeClaim = "banking:principal_type";

    public const string PrincipalNameClaim = "banking:principal_name";
}
