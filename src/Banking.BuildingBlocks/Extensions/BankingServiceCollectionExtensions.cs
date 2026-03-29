using Banking.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Banking.BuildingBlocks.Extensions;

public static class BankingServiceCollectionExtensions
{
    public static IServiceCollection AddBankingApiDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var securityOptions = configuration.GetSection(BankingSecurityOptions.SectionName).Get<BankingSecurityOptions>() ?? new();
        var authenticationEnabled = securityOptions.Authentication.Enabled && !environment.IsEnvironment("Testing");

        // This shared extension keeps each microservice startup intentionally thin and consistent.
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.Configure<BankingSecurityOptions>(configuration.GetSection(BankingSecurityOptions.SectionName));
        services.Configure<BankingSecurityRuntimeOptions>(options => options.AuthenticationEnabled = authenticationEnabled);
        services.AddSingleton<BankingSecurityHeaderValidator>();
        services.AddTransient<InternalServiceAuthenticationDelegatingHandler>();

        services
            .AddAuthentication(BankingAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, BankingHeaderAuthenticationHandler>(
                BankingAuthenticationDefaults.SchemeName,
                _ => { });

        services.AddAuthorization(options =>
        {
            if (authenticationEnabled)
            {
                // By default, business endpoints require auth unless a service explicitly opts out.
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(BankingAuthenticationDefaults.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();

                options.AddPolicy(BankingPolicies.ExternalOrInternal, policy =>
                    policy.AddAuthenticationSchemes(BankingAuthenticationDefaults.SchemeName)
                        .RequireAuthenticatedUser());

                options.AddPolicy(BankingPolicies.ExternalClientOnly, policy =>
                    policy.AddAuthenticationSchemes(BankingAuthenticationDefaults.SchemeName)
                        .RequireAuthenticatedUser()
                        .RequireRole(BankingPrincipalTypes.ExternalClient));

                options.AddPolicy(BankingPolicies.InternalServiceOnly, policy =>
                    policy.AddAuthenticationSchemes(BankingAuthenticationDefaults.SchemeName)
                        .RequireAuthenticatedUser()
                        .RequireRole(BankingPrincipalTypes.InternalService));
            }
            else
            {
                // Testing disables auth globally so test cases can focus on business behavior.
                options.AddPolicy(BankingPolicies.ExternalOrInternal, policy => policy.RequireAssertion(_ => true));
                options.AddPolicy(BankingPolicies.ExternalClientOnly, policy => policy.RequireAssertion(_ => true));
                options.AddPolicy(BankingPolicies.InternalServiceOnly, policy => policy.RequireAssertion(_ => true));
            }
        });

        return services;
    }
}
