using System.Net;
using System.Net.Http.Json;
using Banking.Services.Audit.Contracts;
using FluentAssertions;

namespace Banking.Services.Audit.IntegrationTests;

public sealed class AuditsApiTests : IClassFixture<AuditServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuditsApiTests(AuditServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostAudits_Should_ReturnCreated_When_RequestIsValid()
    {
        var request = new CreateAuditLogRequest(
            "User",
            "user_001",
            "DepositSucceeded",
            "DepositTransaction",
            "dep_001",
            new Dictionary<string, object?> { ["status"] = "Processing" },
            new Dictionary<string, object?> { ["status"] = "Succeeded" },
            "corr-audit-001",
            "cmd-audit-001");

        var response = await _client.PostAsJsonAsync("/api/v1/audits", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var audit = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("DepositSucceeded");
    }

    [Fact]
    public async Task GetAudits_Should_ReturnRecordedAudits()
    {
        var request = new CreateAuditLogRequest(
            "User",
            "user_002",
            "AccountOpened",
            "Account",
            "acc_001",
            null,
            new Dictionary<string, object?> { ["status"] = "Active" },
            "corr-audit-002",
            "cmd-audit-002");

        await _client.PostAsJsonAsync("/api/v1/audits", request);
        var response = await _client.GetAsync("/api/v1/audits?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("AccountOpened");
    }
}
