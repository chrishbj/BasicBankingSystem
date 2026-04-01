namespace Banking.Bff.CustomerPortal.Contracts;

public sealed record CustomerPortalSignInRequest(
    string CustomerNumber,
    string IdentityLast4);
