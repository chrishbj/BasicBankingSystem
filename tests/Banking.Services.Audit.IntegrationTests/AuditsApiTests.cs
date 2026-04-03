using System.Net;
using System.Net.Http.Json;
using Banking.Services.Audit.Contracts;
using Banking.Services.Audit.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

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
        var request = AuditApiTestData.CreateAudit();

        var response = await _client.PostAsJsonAsync("/api/v1/audits", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var audit = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("DepositSucceeded");
    }

    [Fact]
    public async Task PostAudits_Should_ReturnBadRequestProblemDetails_When_ActionIsMissing()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/audits",
            new CreateAuditLogRequest(
                "User",
                "user_001",
                "",
                "DepositTransaction",
                "dep_001",
                null,
                null,
                "corr-audit-invalid-001",
                null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Invalid audit log request");
        problem.Detail.Should().Contain("Action is required");
    }

    [Fact]
    public async Task GetAudits_Should_ReturnRecordedAudits()
    {
        var request = AuditApiTestData.CreateAudit("AccountOpened");

        await _client.PostAsJsonAsync("/api/v1/audits", request);
        var response = await _client.GetAsync("/api/v1/audits?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<AuditSummaryResponse>>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(item => item.Action == "AccountOpened");
        payload.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAuditById_Should_ReturnNotFound_When_AuditDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/v1/audits/aud_missing_{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAudits_Should_ReturnEmptyItems_When_PageIsOutOfRange()
    {
        await _client.PostAsJsonAsync("/api/v1/audits", AuditApiTestData.CreateAudit());

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<AuditSummaryResponse>>(
            "/api/v1/audits?pageNumber=2&pageSize=20");

        response.Should().NotBeNull();
        response!.TotalCount.Should().BeGreaterThan(0);
        response.Items.Should().BeEmpty();
    }
}
