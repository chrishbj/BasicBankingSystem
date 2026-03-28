namespace Banking.BuildingBlocks.Security;

public static class BankingPrincipalTypes
{
    public const string ExternalClient = "external-client";

    public const string InternalService = "internal-service";

    public const string PrincipalTypeClaim = "banking:principal_type";

    public const string PrincipalNameClaim = "banking:principal_name";
}
