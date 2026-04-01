using System.Net;
using System.Net.Http.Json;
using Banking.Gateway.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Banking.Gateway.IntegrationTests;

public sealed class GatewayApiTests : IClassFixture<GatewayWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly GatewayWebApplicationFactory _factory;

    public GatewayApiTests(GatewayWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetHealthSummary_Should_ReportAllDownstreamServices()
    {
        var summary = await _client.GetFromJsonAsync<GatewayHealthSummaryResponse>("/api/v1/system/health-summary");

        summary.Should().NotBeNull();
        summary!.Gateway.Should().Be("Banking.Gateway");
        summary.Services.Should().HaveCount(4);
        summary.Services.Should().OnlyContain(item => item.Health == "Healthy");
    }

    [Fact]
    public async Task GetDocs_Should_RenderUnifiedDocsLandingPage()
    {
        var response = await _client.GetAsync("/api/v1/system/docs");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Banking Gateway");
        content.Should().Contain("/customer-api/swagger");
        content.Should().Contain("/audit-api/openapi/v1.json");
    }

    [Fact]
    public async Task ProxyCustomer_Should_ForwardAuthorizedRequest_WithInternalServiceHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/customer-api/api/v1/customers?pageNumber=1&pageSize=1");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Gateway Demo Customer");

        _factory.CustomerStub.Requests.Should().NotBeEmpty();
        var proxied = _factory.CustomerStub.Requests.Last();
        proxied.PathAndQuery.Should().Be("/api/v1/customers?pageNumber=1&pageSize=1");
        proxied.Headers.Should().ContainKey("X-Service-Name");
        proxied.Headers["X-Service-Name"].Should().ContainSingle("gateway-service");
        proxied.Headers.Should().ContainKey("X-Service-Key");
        proxied.Headers["X-Service-Key"].Should().ContainSingle("gateway-service-dev-key");
    }
}
