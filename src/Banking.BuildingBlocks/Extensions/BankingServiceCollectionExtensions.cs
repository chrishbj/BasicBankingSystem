using Microsoft.Extensions.DependencyInjection;

namespace Banking.BuildingBlocks.Extensions;

public static class BankingServiceCollectionExtensions
{
    public static IServiceCollection AddBankingApiDefaults(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();

        return services;
    }
}
