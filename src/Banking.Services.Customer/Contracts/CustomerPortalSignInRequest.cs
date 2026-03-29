namespace Banking.Services.Customer.Contracts;

public sealed record CustomerPortalSignInRequest(
    string CustomerNumber,
    string IdentityLast4);
