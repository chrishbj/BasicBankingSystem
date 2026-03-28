using Banking.BuildingBlocks.Extensions;
using Banking.Services.Audit.Repositories;
using Banking.Services.Audit.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBankingApiDefaults();

app.Run();

public partial class Program;
