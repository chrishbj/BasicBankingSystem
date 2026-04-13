using Banking.Bff.CustomerPortal.Auth;
using Banking.Bff.CustomerPortal.Clients;
using Banking.Bff.CustomerPortal.Options;
using Banking.BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddBankingRequestProtection(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<DownstreamServiceOptions>(builder.Configuration.GetSection(DownstreamServiceOptions.SectionName));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".basicbanking.customerportal.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services
    .AddAuthentication(CustomerPortalSessionAuthenticationDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, CustomerPortalSessionAuthenticationHandler>(
        CustomerPortalSessionAuthenticationDefaults.SchemeName,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(CustomerPortalSessionAuthenticationDefaults.SchemeName)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICustomerPortalSessionService, CustomerPortalSessionService>();
builder.Services.AddScoped<ICurrentPortalCustomerAccessor, CurrentPortalCustomerAccessor>();

builder.Services.AddHttpClient<CustomerServiceClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.CustomerServiceBaseUrl);
    client.DefaultRequestHeaders.Add("X-Api-Key", options.ExternalApiKey);
});

builder.Services.AddHttpClient<AccountServiceClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.AccountServiceBaseUrl);
    client.DefaultRequestHeaders.Add("X-Api-Key", options.ExternalApiKey);
});

builder.Services.AddHttpClient<DepositServiceClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.DepositServiceBaseUrl);
    client.DefaultRequestHeaders.Add("X-Api-Key", options.ExternalApiKey);
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
}

app.UseExceptionHandler();
app.UseBankingRequestProtection();
app.UseHttpsRedirection();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/v1/health").AllowAnonymous();
app.MapGet("/api/v1/ready", () => Results.Ok(new
{
    status = "ready",
    dependencies = new Dictionary<string, string>
    {
        ["self"] = "ok"
    }
})).AllowAnonymous();

app.Run();

public partial class Program;
