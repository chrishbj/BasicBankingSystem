namespace Banking.BuildingBlocks.Security;

public sealed record BankingSecurityValidationResult(
    bool Succeeded,
    string? PrincipalType,
    string? PrincipalName,
    string? FailureMessage)
{
    public static BankingSecurityValidationResult Success(string principalType, string principalName)
        => new(true, principalType, principalName, null);

    public static BankingSecurityValidationResult NoCredentials()
        => new(false, null, null, null);

    public static BankingSecurityValidationResult Fail(string failureMessage)
        => new(false, null, null, failureMessage);
}
