using Banking.BuildingBlocks.Security;
using Banking.BuildingBlocks.Swagger;
using Banking.BuildingBlocks.Extensions;
using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Auditing;
using Banking.Services.Deposit.Data;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;
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

builder.Services.AddScoped<IDepositRepository, EfDepositRepository>();
builder.Services.AddScoped<IDepositService, DepositService>();
builder.Services.AddScoped<IDepositTransactionProcessor, DepositTransactionProcessor>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<AuditServiceOptions>(builder.Configuration.GetSection(AuditServiceOptions.SectionName));
builder.Services.Configure<AccountServiceOptions>(builder.Configuration.GetSection(AccountServiceOptions.SectionName));

if (isTesting)
{
    builder.Services.AddSingleton<IDepositAccountDirectory, InMemoryDepositAccountDirectory>();
    builder.Services.AddSingleton<IAuditLogWriter, NullAuditLogWriter>();
}
else
{
    builder.Services.AddHttpClient<IDepositAccountDirectory, HttpDepositAccountDirectory>((serviceProvider, httpClient) =>
    {
        var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AccountServiceOptions>>().Value;
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    })
    .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();

    builder.Services.AddHttpClient<IAuditLogWriter, HttpAuditLogWriter>((serviceProvider, httpClient) =>
    {
        var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuditServiceOptions>>().Value;
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    })
    .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();
}

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
    var dbContext = scope.ServiceProvider.GetRequiredService<DepositDbContext>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}
else
{
    await app.Services.EnsureContextObjectsCreatedAsync<DepositDbContext>();
}

app.Run();

public partial class Program;
