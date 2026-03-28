using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Banking.Services.Deposit.IntegrationTests;

public sealed class DepositServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"basicbanking-deposit-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:Postgres:Provider"] = "Sqlite",
                ["Infrastructure:Postgres:SqliteTestingConnectionString"] = $"Data Source={_databasePath}",
                ["Infrastructure:RabbitMq:Transport"] = "InMemory",
                ["Infrastructure:RabbitMq:PollingIntervalMilliseconds"] = "50"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
