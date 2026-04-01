using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Banking.Bff.CustomerPortal.Auth;

public sealed class CustomerPortalSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> authenticationOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder urlEncoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(authenticationOptions, loggerFactory, urlEncoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var customerId = Context.Session.GetString(CustomerPortalSessionKeys.CustomerId);
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var customerNumber = Context.Session.GetString(CustomerPortalSessionKeys.CustomerNumber) ?? string.Empty;
        var fullName = Context.Session.GetString(CustomerPortalSessionKeys.FullName) ?? customerNumber;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, fullName),
            new(CustomerPortalClaims.CustomerId, customerId),
            new(CustomerPortalClaims.CustomerNumber, customerNumber),
            new(ClaimTypes.Role, CustomerPortalClaims.CustomerRole)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
