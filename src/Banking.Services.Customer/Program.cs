using Banking.BuildingBlocks.Swagger;
using Banking.BuildingBlocks.Extensions;
using Banking.Services.Customer.Data;
using Banking.Services.Customer.Repositories;
using Banking.Services.Customer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// AddBankingApiDefaults centralizes the shared cross-cutting concerns used by all services:
// controllers, ProblemDetails, health checks, auth, authorization, and service identity wiring.
builder.Services.AddBankingApiDefaults(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.OperationFilter<BankingSecurityHeadersOperationFilter>());

var isTesting = builder.Environment.IsEnvironment("Testing");
var provider = isTesting
    ? "Sqlite"
    : builder.Configuration["Infrastructure:Postgres:Provider"] ?? "Npgsql";

var connectionString = isTesting
    ? builder.Configuration["Infrastructure:Postgres:SqliteTestingConnectionString"] ?? "Data Source=customer-testing.db"
    : builder.Configuration["Infrastructure:Postgres:ConnectionString"];

builder.Services.AddDbContext<CustomerDbContext>(options =>
{
    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    // Local Swagger is intentionally always available in dev-like environments so the
    // repo can be explored as a portfolio/demo project without extra tooling.
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseBankingApiDefaults();

if (isTesting)
{
    // Test hosts rebuild schema from scratch to keep integration tests deterministic.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}
else
{
    // Local runtime bootstraps context-owned objects automatically instead of depending
    // on a separate migration/deployment step.
    await app.Services.EnsureContextObjectsCreatedAsync<CustomerDbContext>();
}

app.Run();

public partial class Program;
