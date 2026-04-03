using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Banking.Testing.Shared;

public abstract class SqliteWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string _databasePath;

    protected SqliteWebApplicationFactory(string databaseNamePrefix)
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"{databaseNamePrefix}-{Guid.NewGuid():N}.db");
    }

    protected string DatabasePath => _databasePath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Infrastructure:Postgres:Provider"] = "Sqlite",
                ["Infrastructure:Postgres:SqliteTestingConnectionString"] = $"Data Source={_databasePath}"
            };

            foreach (var setting in GetAdditionalConfiguration())
            {
                settings[setting.Key] = setting.Value;
            }

            configBuilder.AddInMemoryCollection(settings);
        });
    }

    protected virtual IReadOnlyDictionary<string, string?> GetAdditionalConfiguration()
        => new Dictionary<string, string?>();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
