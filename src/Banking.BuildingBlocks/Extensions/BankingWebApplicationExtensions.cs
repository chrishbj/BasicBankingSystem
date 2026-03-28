using Banking.BuildingBlocks.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Banking.BuildingBlocks.Extensions;

public static class BankingWebApplicationExtensions
{
    public static WebApplication UseBankingApiDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/api/v1/health");
        app.MapGet("/api/v1/ready", () => Results.Ok(new
        {
            status = "ready",
            dependencies = new Dictionary<string, string>
            {
                ["self"] = "ok"
            }
        }));

        return app;
    }
}
