namespace Banking.BuildingBlocks.Security;

public sealed record BankingSecurityValidationResult(
    bool Succeeded,
    string? PrincipalType,
    string? PrincipalName,
    IReadOnlyCollection<string> Roles,
    string? FailureMessage)
{
    public static BankingSecurityValidationResult Success(
        string principalType,
        string principalName,
        IReadOnlyCollection<string>? roles = null)
        => new(true, principalType, principalName, roles ?? [], null);

    public static BankingSecurityValidationResult NoCredentials()
        => new(false, null, null, [], null);

    public static BankingSecurityValidationResult Fail(string failureMessage)
        => new(false, null, null, [], failureMessage);
}
