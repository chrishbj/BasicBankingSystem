namespace Banking.BuildingBlocks.Security;

public static class BankingPolicies
{
    public const string ExternalOrInternal = "BankingExternalOrInternal";
    public const string ExternalClientOnly = "BankingExternalClientOnly";
    public const string InternalServiceOnly = "BankingInternalServiceOnly";
    public const string CustomerOnly = "BankingCustomerOnly";
    public const string BusinessUserOnly = "BankingBusinessUserOnly";
    public const string PlatformReadOnly = "BankingPlatformReadOnly";
    public const string PlatformOperatorOnly = "BankingPlatformOperatorOnly";
    public const string SecurityAdministratorOnly = "BankingSecurityAdministratorOnly";
    public const string PrivilegedMaintenanceAction = "BankingPrivilegedMaintenanceAction";
}
