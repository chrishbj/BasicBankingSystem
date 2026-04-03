using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Banking.BuildingBlocks.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Gateway.IntegrationTests;

public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    public RecordingDownstreamStub CustomerStub { get; } = new("customer");
    public RecordingDownstreamStub AccountStub { get; } = new("account");
    public RecordingDownstreamStub DepositStub { get; } = new("deposit");
    public RecordingDownstreamStub AuditStub { get; } = new("audit");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:CustomerServiceBaseUrl"] = "http://customer.test/",
                ["Infrastructure:AccountServiceBaseUrl"] = "http://account.test/",
                ["Infrastructure:DepositServiceBaseUrl"] = "http://deposit.test/",
                ["Infrastructure:AuditServiceBaseUrl"] = "http://audit.test/",
                ["Security:Authentication:ExternalApiKeys:0:Name"] = "local-dev-client",
                ["Security:Authentication:ExternalApiKeys:0:ApiKey"] = "local-dev-api-key",
                ["Security:Authentication:InternalServices:0:Name"] = "gateway-service",
                ["Security:Authentication:InternalServices:0:ApiKey"] = "gateway-service-dev-key",
                ["Security:CurrentServiceIdentity:ServiceName"] = "gateway-service",
                ["Security:CurrentServiceIdentity:ApiKey"] = "gateway-service-dev-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddHttpClient("customer-service", client => client.BaseAddress = new Uri("http://customer.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(CustomerStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();

            services.AddHttpClient("account-service", client => client.BaseAddress = new Uri("http://account.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(AccountStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();

            services.AddHttpClient("deposit-service", client => client.BaseAddress = new Uri("http://deposit.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(DepositStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();

            services.AddHttpClient("audit-service", client => client.BaseAddress = new Uri("http://audit.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(AuditStub.HandleAsync))
                .AddHttpMessageHandler<InternalServiceAuthenticationDelegatingHandler>();
        });
    }
}

public sealed class RecordingDownstreamStub(string serviceName)
{
    public ConcurrentQueue<RecordedDownstreamRequest> Requests { get; } = new();
    public HttpStatusCode? ForcedStatusCode { get; set; }
    public string? ForcedContent { get; set; }

    public Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(new RecordedDownstreamRequest(
            request.Method.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty,
            request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase)));

        if (ForcedStatusCode is not null)
        {
            return Task.FromResult(new HttpResponseMessage(ForcedStatusCode.Value)
            {
                Content = new StringContent(ForcedContent ?? string.Empty)
            });
        }

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (path == "/api/v1/health")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Healthy")
            });
        }

        if (serviceName == "customer" && path == "/api/v1/customers")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = new[]
                    {
                        new
                        {
                            customerNumber = "C2026033114163272720",
                            fullName = "Gateway Demo Customer"
                        }
                    },
                    pageNumber = 1,
                    pageSize = 1,
                    totalCount = 1,
                    totalPages = 1
                })
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public sealed record RecordedDownstreamRequest(
    string Method,
    string PathAndQuery,
    IReadOnlyDictionary<string, string[]> Headers);

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        handler(request, cancellationToken);
}
