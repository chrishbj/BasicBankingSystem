using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Banking.BuildingBlocks.Security;

public sealed class BankingHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> authenticationOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder urlEncoder,
    BankingSecurityHeaderValidator headerValidator,
    IOptions<BankingSecurityRuntimeOptions> runtimeOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(authenticationOptions, loggerFactory, urlEncoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!runtimeOptions.Value.AuthenticationEnabled)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Header validation allows the same authentication handler to support both
        // external API-key clients and internal service-to-service callers.
        var validationResult = headerValidator.Validate(Request.Headers);
        if (!validationResult.Succeeded)
        {
            if (string.IsNullOrWhiteSpace(validationResult.FailureMessage))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            return Task.FromResult(AuthenticateResult.Fail(validationResult.FailureMessage));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, validationResult.PrincipalName!),
            new(BankingPrincipalTypes.PrincipalTypeClaim, validationResult.PrincipalType!),
            new(BankingPrincipalTypes.PrincipalNameClaim, validationResult.PrincipalName!)
        };

        claims.Add(new Claim(ClaimTypes.Role, validationResult.PrincipalType!));

        foreach (var role in validationResult.Roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.Ordinal))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
