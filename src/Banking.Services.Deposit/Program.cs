using Banking.BuildingBlocks.Extensions;
using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDepositAccountDirectory, InMemoryDepositAccountDirectory>();
builder.Services.AddSingleton<IDepositRepository, InMemoryDepositRepository>();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBankingApiDefaults();

app.Run();

public partial class Program;
