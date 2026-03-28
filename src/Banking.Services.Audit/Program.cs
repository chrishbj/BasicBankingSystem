using Banking.BuildingBlocks.Extensions;
using Banking.Services.Audit.Data;
using Banking.Services.Audit.Repositories;
using Banking.Services.Audit.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var isTesting = builder.Environment.IsEnvironment("Testing");
var provider = isTesting
    ? "Sqlite"
    : builder.Configuration["Infrastructure:Postgres:Provider"] ?? "Npgsql";

var connectionString = isTesting
    ? builder.Configuration["Infrastructure:Postgres:SqliteTestingConnectionString"] ?? "Data Source=audit-testing.db"
    : builder.Configuration["Infrastructure:Postgres:ConnectionString"];

builder.Services.AddDbContext<AuditDbContext>(options =>
{
    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IAuditRepository, EfAuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseBankingApiDefaults();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

public partial class Program;
