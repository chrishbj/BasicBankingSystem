using System.Threading.RateLimiting;
using Banking.BuildingBlocks.Resilience;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Banking.BuildingBlocks.Extensions;

public static class RequestProtectionExtensions
{
    public static IServiceCollection AddBankingRequestProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddMemoryCache();
        var settings = configuration.GetSection(RequestProtectionOptions.SectionName).Get<RequestProtectionOptions>()
            ?? new RequestProtectionOptions();
        services.Configure<RequestProtectionOptions>(configuration.GetSection(RequestProtectionOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers["Retry-After"] =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    title = "Too many requests",
                    detail = "The caller exceeded the configured request rate. Please retry later.",
                    status = StatusCodes.Status429TooManyRequests
                }, cancellationToken));
            };

            if (!settings.EnableGlobalRateLimiting || environment.IsEnvironment("Testing"))
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("request-protection-disabled"));
                return;
            }

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var partitionKey =
                    context.User.Identity?.Name ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.GlobalPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.GlobalWindowSeconds),
                        QueueLimit = settings.GlobalQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    public static WebApplication UseBankingRequestProtection(this WebApplication app)
    {
        app.UseRateLimiter();
        app.UseMiddleware<IdempotencyReplayProtectionMiddleware>();
        return app;
    }
}
