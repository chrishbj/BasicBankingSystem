using Banking.BuildingBlocks.Extensions;
using Banking.Services.Deposit.Accounts;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDepositAccountDirectory, InMemoryDepositAccountDirectory>();
builder.Services.AddSingleton<IDepositRepository, InMemoryDepositRepository>();
builder.Services.AddScoped<IDepositService, DepositService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBankingApiDefaults();

app.Run();

public partial class Program;
