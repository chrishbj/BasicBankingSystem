using Banking.BuildingBlocks.Extensions;
using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Data;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults();
builder.Services.AddOpenApi();

var isTesting = builder.Environment.IsEnvironment("Testing");
var provider = isTesting
    ? "Sqlite"
    : builder.Configuration["Infrastructure:Postgres:Provider"] ?? "Npgsql";

var connectionString = isTesting
    ? builder.Configuration["Infrastructure:Postgres:SqliteTestingConnectionString"] ?? "Data Source=deposit-testing.db"
    : builder.Configuration["Infrastructure:Postgres:ConnectionString"];

builder.Services.AddDbContext<DepositDbContext>(options =>
{
    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<IDepositAccountDirectory, InMemoryDepositAccountDirectory>();
builder.Services.AddScoped<IDepositRepository, EfDepositRepository>();
builder.Services.AddScoped<IDepositService, DepositService>();
builder.Services.AddScoped<IDepositTransactionProcessor, DepositTransactionProcessor>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var transport = builder.Environment.IsEnvironment("Testing")
    ? DepositMessageTransport.InMemory
    : builder.Configuration["Infrastructure:RabbitMq:Transport"] ?? DepositMessageTransport.RabbitMq;

if (string.Equals(transport, DepositMessageTransport.InMemory, StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<InMemoryDepositMessageQueue>();
    builder.Services.AddSingleton<IDepositEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<InMemoryDepositMessageQueue>());
    builder.Services.AddHostedService<InMemoryDepositMessageConsumer>();
}
else
{
    builder.Services.AddSingleton<IDepositEventPublisher, RabbitMqDepositEventPublisher>();
    builder.Services.AddHostedService<RabbitMqDepositMessageConsumer>();
}

builder.Services.AddHostedService<DepositOutboxDispatcher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBankingApiDefaults();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DepositDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

public partial class Program;
