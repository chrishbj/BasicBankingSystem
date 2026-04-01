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
    public async Task GetPlatformOverview_Should_ReturnServiceAndDepositWorkflowSummary()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/overview");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");

        var response = await _client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<PlatformOverviewResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        overview.Should().NotBeNull();
        overview!.Platform.Should().Be("BasicBankingSystem");
        overview.Services.Should().HaveCount(4);
        overview.Deposits.ReceivedCount.Should().Be(2);
        overview.Deposits.SucceededCount.Should().Be(14);
        overview.Deposits.FailedCount.Should().Be(1);
        overview.DepositRuntime.MessageTransport.Should().Be("InMemory");
        overview.Deposits.PendingReviewCount.Should().Be(1);
        overview.Deposits.PendingReviewItems.Should().ContainSingle(item => item.TransactionId == "dep-review-001");
    }

    [Fact]
    public async Task GetDepositRuntimeStatus_Should_ReturnWorkerAndBacklogSignals()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/workflows/deposits/runtime");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");

        var response = await _client.SendAsync(request);
        var runtime = await response.Content.ReadFromJsonAsync<DepositRuntimeStatusResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        runtime.Should().NotBeNull();
        runtime!.PendingReviewCount.Should().Be(1);
        runtime.PendingOutboxCount.Should().Be(1);
        runtime.Workers.Should().Contain(item => item.WorkerName == "DepositOutboxDispatcher" && item.BacklogCount == 1);
    }

    [Fact]
    public async Task GetCorrelationDiagnostics_Should_ReturnMatchingDepositsAndAuditEvents()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/diagnostics/correlation/corr-platform-001");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");

        var response = await _client.SendAsync(request);
        var diagnostics = await response.Content.ReadFromJsonAsync<CorrelationDiagnosticsResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        diagnostics.Should().NotBeNull();
        diagnostics!.CorrelationId.Should().Be("corr-platform-001");
        diagnostics.Deposits.Should().ContainSingle(item => item.TransactionId == "dep-platform-001");
        diagnostics.AuditEvents.Should().HaveCount(5);
        diagnostics.AuditEvents.Should().OnlyContain(item => item.CorrelationId == "corr-platform-001");
    }

    [Fact]
    public async Task GetDepositWorkflowDetail_Should_ReturnDepositState()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/workflows/deposits/dep-platform-001");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");

        var response = await _client.SendAsync(request);
        var detail = await response.Content.ReadFromJsonAsync<DepositWorkflowDetailResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        detail.Should().NotBeNull();
        detail!.CorrelationId.Should().Be("corr-platform-001");
        detail.Status.Should().Be("PendingReview");
        detail.AuditStatus.Should().Be("Failed");
    }

    [Fact]
    public async Task RetryDepositCompensation_Should_InvokeDepositMaintenancePath_And_RecordAudit()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/maintenance/deposits/dep-platform-001/retry-compensation");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");
        request.Content = JsonContent.Create(new
        {
            reason = "Platform maintenance retry"
        });

        var response = await _client.SendAsync(request);
        var action = await response.Content.ReadFromJsonAsync<PlatformMaintenanceActionResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        action.Should().NotBeNull();
        action!.ActionType.Should().Be("RetryDepositCompensation");
        action.OperationId.Should().Be("aud-platform-maint-001");
        action.ResultStatus.Should().Be("Succeeded");

        _factory.DepositStub.Requests.Should().ContainSingle(item =>
            item.Method == "POST" &&
            item.PathAndQuery == "/api/v1/deposits/dep-platform-001/review/retry-compensation");

        _factory.AuditStub.Requests.Should().Contain(item =>
            item.Method == "POST" &&
            item.PathAndQuery == "/api/v1/audits");
    }

    [Fact]
    public async Task ResolveDepositReview_Should_InvokeDepositResolvePath_And_RecordAudit()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/maintenance/deposits/dep-platform-001/resolve-review");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");
        request.Content = JsonContent.Create(new
        {
            resolution = "ReversedExternally",
            reason = "Platform resolved pending review"
        });

        var response = await _client.SendAsync(request);
        var action = await response.Content.ReadFromJsonAsync<PlatformMaintenanceActionResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        action.Should().NotBeNull();
        action!.ActionType.Should().Be("ResolveDepositReview");
        action.OperationId.Should().Be("aud-platform-maint-002");
        action.ResultStatus.Should().Be("Succeeded");

        _factory.DepositStub.Requests.Should().Contain(item =>
            item.Method == "POST" &&
            item.PathAndQuery == "/api/v1/deposits/dep-platform-001/review/resolve");

        _factory.AuditStub.Requests.Should().Contain(item =>
            item.Method == "POST" &&
            item.PathAndQuery == "/api/v1/audits");
    }

    [Fact]
    public async Task GetPlatformOperationsAudit_Should_ReturnPlatformMaintenanceEvents()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/audit/operations");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");

        var response = await _client.SendAsync(request);
        var auditItems = await response.Content.ReadFromJsonAsync<List<AuditTraceItemResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        auditItems.Should().NotBeNull();
        auditItems!.Should().Contain(item => item.Action == "PlatformMaintenanceRetryCompensation");
        auditItems.Should().Contain(item => item.Action == "PlatformMaintenanceResolveDepositReview");
        auditItems.Should().Contain(item => item.Action == "PlatformMaintenanceRequeueOutboxMessage");
        auditItems.Should().OnlyContain(item => item.Action.StartsWith("Platform"));
    }

    [Fact]
    public async Task GetDepositOutboxMessages_Should_ReturnOutboxVisibility()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/workflows/deposits/outbox");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");

        var response = await _client.SendAsync(request);
        var outboxItems = await response.Content.ReadFromJsonAsync<List<DepositOutboxMessageItemResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        outboxItems.Should().NotBeNull();
        outboxItems!.Should().HaveCount(2);
        outboxItems.Should().Contain(item => item.MessageId == "out-platform-002" && item.ProcessedAt == null);
    }

    [Fact]
    public async Task RequeueOutboxMessage_Should_InvokeDepositOutboxRequeue_And_RecordAudit()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/maintenance/deposits/outbox/out-platform-002/requeue");
        request.Headers.Add("X-Api-Key", "local-dev-api-key");
        request.Content = JsonContent.Create(new
        {
            reason = "Platform outbox requeue"
        });

        var response = await _client.SendAsync(request);
        var action = await response.Content.ReadFromJsonAsync<PlatformMaintenanceActionResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        action.Should().NotBeNull();
        action!.ActionType.Should().Be("RequeueOutboxMessage");
        action.OperationId.Should().Be("aud-platform-maint-003");

        _factory.DepositStub.Requests.Should().Contain(item =>
            item.Method == "POST" &&
            item.PathAndQuery == "/api/v1/deposits/outbox/out-platform-002/requeue");
    }

    [Fact]
    public async Task GetPlatformServices_Should_RejectUnauthorizedCaller()
    {
        var response = await _client.GetAsync("/api/platform/services");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDocs_Should_RenderUnifiedDocsLandingPage()
    {
        var response = await _client.GetAsync("/api/v1/system/docs");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Banking Gateway");
        content.Should().Contain("/customer-api/swagger/index.html");
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

    [Fact]
    public async Task SwaggerShortcut_Should_RedirectToIndexHtml()
    {
        var response = await _client.GetAsync("/customer-api/swagger");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be("/customer-api/swagger/index.html");
    }
}
