using Banking.BuildingBlocks.Security;
using Banking.BuildingBlocks.Swagger;
using Banking.BuildingBlocks.Extensions;
using Banking.Services.Account.CustomerDirectory;
using Banking.Services.Account.Data;
using Banking.Services.Account.Repositories;
using Banking.Services.Account.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.OperationFilter<BankingSecurityHeadersOperationFilter>());

var isTesting = builder.Environment.IsEnvironment("Testing");
var provider = isTesting
    ? "Sqlite"
    : builder.Configuration["Infrastructure:Postgres:Provider"] ?? "Npgsql";

var connectionString = isTesting
    ? builder.Configuration["Infrastructure:Postgres:SqliteTestingConnectionString"] ?? "Data Source=account-testing.db"
    : builder.Configuration["Infrastructure:Postgres:ConnectionString"];

builder.Services.AddDbContext<AccountDbContext>(options =>
{
    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    options.UseNpgsql(connectionString);
});

builder.Services.Configure<CustomerServiceOptions>(builder.Configuration.GetSection(CustomerServiceOptions.SectionName));

if (isTesting)
{
    builder.Services.AddSingleton<ICustomerDirectory, InMemoryCustomerDirectory>();
}
else
{
    builder.Services.AddHttpClient<ICustomerDirectory, HttpCustomerDirectory>((serviceProvider, httpClient) =>
    {
        var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CustomerServiceOptions>>().Value;
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    })
    .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();
}

builder.Services.AddScoped<IAccountRepository, EfAccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseBankingApiDefaults();

if (isTesting)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}
else
{
    await app.Services.EnsureContextObjectsCreatedAsync<AccountDbContext>();
}

app.Run();

public partial class Program;
