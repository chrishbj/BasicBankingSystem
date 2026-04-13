using Banking.BuildingBlocks.Extensions;
using Banking.Gateway.Options;
using Banking.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.Configure<GatewayDownstreamOptions>(builder.Configuration.GetSection(GatewayDownstreamOptions.SectionName));
builder.Services.AddHttpClient("customer-service", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayDownstreamOptions>>().Value;
    client.BaseAddress = new Uri(options.CustomerServiceBaseUrl);
}).AddHttpMessageHandler<Banking.BuildingBlocks.Security.InternalServiceAuthenticationDelegatingHandler>();
builder.Services.AddHttpClient("account-service", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayDownstreamOptions>>().Value;
    client.BaseAddress = new Uri(options.AccountServiceBaseUrl);
}).AddHttpMessageHandler<Banking.BuildingBlocks.Security.InternalServiceAuthenticationDelegatingHandler>();
builder.Services.AddHttpClient("deposit-service", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayDownstreamOptions>>().Value;
    client.BaseAddress = new Uri(options.DepositServiceBaseUrl);
}).AddHttpMessageHandler<Banking.BuildingBlocks.Security.InternalServiceAuthenticationDelegatingHandler>();
builder.Services.AddHttpClient("audit-service", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayDownstreamOptions>>().Value;
    client.BaseAddress = new Uri(options.AuditServiceBaseUrl);
}).AddHttpMessageHandler<Banking.BuildingBlocks.Security.InternalServiceAuthenticationDelegatingHandler>();
builder.Services.AddSingleton<GatewayProxyService>();
builder.Services.AddSingleton<GatewayHealthService>();
builder.Services.AddSingleton<PlatformMonitoringService>();
builder.Services.AddSingleton<PlatformMaintenanceService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseBankingApiDefaults();

app.Run();

public partial class Program;
