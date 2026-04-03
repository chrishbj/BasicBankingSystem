using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.Bff.CustomerPortal.Clients;
using Banking.Bff.CustomerPortal.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Bff.CustomerPortal.IntegrationTests;

public sealed class CustomerPortalBffWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string CustomerId = "cus_portal_001";
    public const string CustomerNumber = "C2026033114163272720";
    public const string AccountId = "acc_portal_001";
    public const string AccountNumber = "62222026033114164845175";
    public const string ForeignAccountId = "acc_foreign_001";
    public const string ForeignAccountNumber = "62222026033114164849999";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:CustomerServiceBaseUrl"] = "http://customer.test/",
                ["Infrastructure:AccountServiceBaseUrl"] = "http://account.test/",
                ["Infrastructure:DepositServiceBaseUrl"] = "http://deposit.test/",
                ["Infrastructure:ExternalApiKey"] = "local-dev-api-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<CustomerServiceStub>();
            services.AddSingleton<AccountServiceStub>();
            services.AddSingleton<DepositServiceStub>();

            services.AddHttpClient<CustomerServiceClient>(client =>
                {
                    client.BaseAddress = new Uri("http://customer.test/");
                    client.DefaultRequestHeaders.Add("X-Api-Key", "local-dev-api-key");
                })
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                    new StubHttpMessageHandler(serviceProvider.GetRequiredService<CustomerServiceStub>().HandleAsync));

            services.AddHttpClient<AccountServiceClient>(client =>
                {
                    client.BaseAddress = new Uri("http://account.test/");
                    client.DefaultRequestHeaders.Add("X-Api-Key", "local-dev-api-key");
                })
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                    new StubHttpMessageHandler(serviceProvider.GetRequiredService<AccountServiceStub>().HandleAsync));

            services.AddHttpClient<DepositServiceClient>(client =>
                {
                    client.BaseAddress = new Uri("http://deposit.test/");
                    client.DefaultRequestHeaders.Add("X-Api-Key", "local-dev-api-key");
                })
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                    new StubHttpMessageHandler(serviceProvider.GetRequiredService<DepositServiceStub>().HandleAsync));
        });
    }
}

internal sealed class CustomerServiceStub
{
    private static readonly CustomerResponse Customer = new(
        CustomerPortalBffWebApplicationFactory.CustomerId,
        CustomerPortalBffWebApplicationFactory.CustomerNumber,
        "Portal Demo Customer",
        "NationalId",
        "WITHDR********-001",
        "0001",
        "13800000000",
        "portal@example.com",
        new AddressResponse("US", "New York", "New York", "1 Demo Plaza", "10001"),
        "Low",
        2,
        DateTimeOffset.Parse("2026-03-31T08:00:00-04:00"),
        DateTimeOffset.Parse("2026-03-31T08:30:00-04:00"));

    public async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/api/v1/customers/portal-sign-in")
        {
            var payload = await request.Content!.ReadFromJsonAsync<CustomerPortalSignInRequest>(cancellationToken);
            if (payload?.CustomerNumber == Customer.CustomerNumber && payload.IdentityLast4 == Customer.PortalIdentityLast4)
            {
                return Json(Customer);
            }

            return Problem(HttpStatusCode.Unauthorized, "Invalid portal sign-in", "The supplied customer portal sign-in details are invalid.");
        }

        if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == $"/api/v1/customers/{Customer.CustomerId}")
        {
            return Json(Customer);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json<T>(T payload) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(payload)
    };

    private static HttpResponseMessage Problem(HttpStatusCode statusCode, string title, string detail) => new(statusCode)
    {
        Content = JsonContent.Create(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = (int)statusCode
        })
    };
}

internal sealed class AccountServiceStub
{
    private static readonly AccountResponse Account = new(
        CustomerPortalBffWebApplicationFactory.AccountId,
        CustomerPortalBffWebApplicationFactory.AccountNumber,
        CustomerPortalBffWebApplicationFactory.CustomerId,
        "Checking",
        "USD",
        1,
        1030m,
        1030m,
        DateTimeOffset.Parse("2026-03-31T09:00:00-04:00"),
        null);

    public Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var query = request.RequestUri?.Query ?? string.Empty;

        if (request.Method == HttpMethod.Get &&
            path == "/api/v1/accounts" &&
            query.Contains($"customerId={Uri.EscapeDataString(CustomerPortalBffWebApplicationFactory.CustomerId)}", StringComparison.Ordinal))
        {
            return Task.FromResult(Json(new PagedResponse<AccountSummaryResponse>(
                [new AccountSummaryResponse(Account.AccountId, Account.AccountNumber, Account.AccountType, Account.Currency, Account.Status, Account.AvailableBalance, Account.LedgerBalance)],
                1,
                50,
                1,
                1)));
        }

        if (request.Method == HttpMethod.Get &&
            path == $"/api/v1/accounts/by-number/{CustomerPortalBffWebApplicationFactory.AccountNumber}")
        {
            return Task.FromResult(Json(Account));
        }

        if (request.Method == HttpMethod.Get &&
            path == $"/api/v1/accounts/by-number/{CustomerPortalBffWebApplicationFactory.ForeignAccountNumber}")
        {
            return Task.FromResult(Json(new AccountResponse(
                CustomerPortalBffWebApplicationFactory.ForeignAccountId,
                CustomerPortalBffWebApplicationFactory.ForeignAccountNumber,
                "cus_other_001",
                "Checking",
                "USD",
                1,
                500m,
                500m,
                DateTimeOffset.Parse("2026-03-31T09:00:00-04:00"),
                null)));
        }

        if (request.Method == HttpMethod.Get &&
            path == $"/api/v1/accounts/{CustomerPortalBffWebApplicationFactory.AccountId}/activities")
        {
            return Task.FromResult(Json(new PagedResponse<AccountActivityResponse>(
                [
                    new AccountActivityResponse("PORTAL-DEP-0001", Account.AccountId, 1, 80m, "USD", "portal-corr-1", null, DateTimeOffset.Parse("2026-03-31T10:00:00-04:00")),
                    new AccountActivityResponse("PORTAL-WD-0002", Account.AccountId, 3, 50m, "USD", "portal-corr-2", null, DateTimeOffset.Parse("2026-03-31T11:00:00-04:00"))
                ],
                1,
                5,
                2,
                1)));
        }

        if (request.Method == HttpMethod.Post &&
            path == $"/api/v1/accounts/{CustomerPortalBffWebApplicationFactory.AccountId}/withdrawals")
        {
            return Task.FromResult(Json(Account));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static HttpResponseMessage Json<T>(T payload) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(payload)
    };
}

internal sealed class DepositServiceStub
{
    private static readonly DepositResponse Deposit = new(
        "dep_portal_001",
        "D202603311420531889",
        CustomerPortalBffWebApplicationFactory.CustomerId,
        CustomerPortalBffWebApplicationFactory.AccountId,
        80m,
        "USD",
        "PORTAL-DEP-0001",
        3,
        "portal-corr-1",
        null,
        null,
        DateTimeOffset.Parse("2026-03-31T10:00:00-04:00"),
        DateTimeOffset.Parse("2026-03-31T10:00:05-04:00"));

    public async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var query = request.RequestUri?.Query ?? string.Empty;

        if (request.Method == HttpMethod.Get &&
            path == "/api/v1/deposits" &&
            query.Contains($"customerId={Uri.EscapeDataString(CustomerPortalBffWebApplicationFactory.CustomerId)}", StringComparison.Ordinal))
        {
            return Json(new PagedResponse<DepositSummaryResponse>(
                [new DepositSummaryResponse(
                    Deposit.TransactionId,
                    Deposit.TransactionNumber,
                    Deposit.CustomerId,
                    Deposit.AccountId,
                    Deposit.Amount,
                    Deposit.Currency,
                    Deposit.ReferenceNumber,
                    Deposit.Status,
                    Deposit.RequestedAt,
                    Deposit.PostedAt)],
                1,
                20,
                1,
                1));
        }

        if (request.Method == HttpMethod.Get && path == $"/api/v1/deposits/{Deposit.TransactionId}")
        {
            return Json(Deposit);
        }

        if (request.Method == HttpMethod.Post && path == "/api/v1/deposits")
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("customerId").GetString().Should().Be(CustomerPortalBffWebApplicationFactory.CustomerId);
            document.RootElement.GetProperty("accountId").GetString().Should().Be(CustomerPortalBffWebApplicationFactory.AccountId);

            request.Headers.Contains("Idempotency-Key").Should().BeTrue();
            request.Headers.Contains("X-Correlation-Id").Should().BeTrue();

            return Json(Deposit, HttpStatusCode.Accepted);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode)
    {
        Content = JsonContent.Create(payload)
    };
}

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        handler(request, cancellationToken);
}
