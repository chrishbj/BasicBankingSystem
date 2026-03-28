using Banking.BuildingBlocks.Extensions;
using Banking.Services.Account.CustomerDirectory;
using Banking.Services.Account.Repositories;
using Banking.Services.Account.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICustomerDirectory, InMemoryCustomerDirectory>();
builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBankingApiDefaults();

app.Run();

public partial class Program;
