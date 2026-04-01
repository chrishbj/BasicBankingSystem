using System.Security.Claims;

namespace Banking.Bff.CustomerPortal.Auth;

public static class CustomerPortalClaims
{
    public const string CustomerId = "customer_id";
    public const string CustomerNumber = "customer_number";
    public const string CustomerRole = "Customer";

    public static string? GetCustomerId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(CustomerId);
}
