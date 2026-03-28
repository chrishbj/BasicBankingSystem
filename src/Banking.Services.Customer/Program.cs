using Banking.BuildingBlocks.Extensions;
using Banking.Services.Customer.Repositories;
using Banking.Services.Customer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBankingApiDefaults();

app.Run();

public partial class Program;
