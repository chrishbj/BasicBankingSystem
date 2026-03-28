using Banking.BuildingBlocks.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankingApiDefaults(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBankingApiDefaults();

app.Run();

public partial class Program;
