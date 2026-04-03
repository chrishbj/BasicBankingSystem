using System.Net;
using System.Net.Http.Json;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Banking.Services.Deposit.IntegrationTests.Support;

namespace Banking.Services.Deposit.IntegrationTests;

public sealed class DepositsApiTests : IClassFixture<DepositServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly DepositServiceWebApplicationFactory _factory;
    private readonly DepositApiDriver _driver;

    public DepositsApiTests(DepositServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _driver = new DepositApiDriver(_client, factory);
    }

    [Fact]
    public async Task PostDeposits_Should_ReturnAccepted_When_RequestIsValid()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 500m, "USD", DepositChannel.Counter, null, null);
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deposits")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", "dep-integration-001");

        var response = await _client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var deposit = await response.Content.ReadFromJsonAsync<DepositResponse>();
        deposit.Should().NotBeNull();
        deposit!.Status.Should().BeOneOf(DepositStatus.Received, DepositStatus.Succeeded);

        var completed = deposit.Status == DepositStatus.Succeeded
            ? deposit
            : await _driver.WaitForDepositAsync(deposit.TransactionId, DepositStatus.Succeeded);
        completed.Status.Should().Be(DepositStatus.Succeeded);
        completed.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PostDeposits_Should_ReturnConflict_When_AccountIsFrozen()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_frozen_001", 500m, "USD", DepositChannel.Counter, null, null);
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deposits")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", "dep-integration-002");

        var response = await _client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Invalid deposit request");
    }

    [Fact]
    public async Task PostDeposits_Should_ReturnBadRequestProblemDetails_When_IdempotencyKeyIsMissing()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/deposits",
            new CreateDepositRequest("cus_active_001", "acc_active_001", 500m, "USD", DepositChannel.Counter, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Missing idempotency key");
        problem.Detail.Should().Be("Idempotency-Key header is required.");
    }

    [Fact]
    public async Task ResolvePendingReview_Should_ReturnOk_When_DepositIsManuallyResolved()
    {
        var transactionId = "dep-review-api-001";
        await _driver.SeedPendingReviewDepositAsync(transactionId);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/deposits/{transactionId}/review/resolve",
            new ResolveDepositReviewRequest(
                DepositReviewResolution.ReversedExternally,
                "ops-user",
                "Compensation completed through offline operations."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deposit = await response.Content.ReadFromJsonAsync<DepositResponse>();
        deposit.Should().NotBeNull();
        deposit!.Status.Should().Be(DepositStatus.Reversed);
        deposit.ReviewResolution.Should().Be(DepositReviewResolution.ReversedExternally);
        deposit.ReviewLastActionBy.Should().Be("ops-user");
    }

    [Fact]
    public async Task GetPendingReview_Should_ReturnPendingReviewDeposits()
    {
        var transactionId = "dep-review-api-002";
        await _driver.SeedPendingReviewDepositAsync(transactionId);

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<PendingReviewDepositSummaryResponse>>(
            "/api/v1/deposits/review/pending?pageNumber=1&pageSize=20");

        response.Should().NotBeNull();
        response!.Items.Should().ContainSingle(item => item.TransactionId == transactionId && item.AccountNumber == "6222200000000000001");
    }

    [Fact]
    public async Task GetPendingReview_Should_SupportSortingQueryString()
    {
        await _driver.SeedPendingReviewDepositAsync("dep-review-api-010", new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero));
        await _driver.SeedPendingReviewDepositAsync("dep-review-api-011", new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero));

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<PendingReviewDepositSummaryResponse>>(
            "/api/v1/deposits/review/pending?sortBy=RequestedAt&descending=true&pageNumber=1&pageSize=20");

        response.Should().NotBeNull();
        response!.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        response.Items
            .Where(item => item.TransactionId is "dep-review-api-010" or "dep-review-api-011")
            .Select(item => item.TransactionId)
            .Should()
            .ContainInOrder("dep-review-api-011", "dep-review-api-010");
    }

    [Fact]
    public async Task GetDepositById_Should_ReturnNotFound_When_DepositDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/v1/deposits/dep_missing_{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_Should_FilterByStatus_When_QueryStringProvided()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 320m, "USD", DepositChannel.Counter, null, null);
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deposits")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", "dep-integration-filter-001");

        var createResponse = await _client.SendAsync(message);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var created = await createResponse.Content.ReadFromJsonAsync<DepositResponse>();
        created.Should().NotBeNull();

        var completed = created!.Status == DepositStatus.Succeeded
            ? created
            : await _driver.WaitForDepositAsync(created.TransactionId, DepositStatus.Succeeded);

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<DepositSummaryResponse>>(
            "/api/v1/deposits?status=Succeeded&pageNumber=1&pageSize=20");

        response.Should().NotBeNull();
        response!.Items.Should().Contain(item => item.TransactionId == completed.TransactionId);
        response.Items.Should().OnlyContain(item => item.Status == DepositStatus.Succeeded);
    }

    [Fact]
    public async Task GetAll_Should_FilterByCorrelationId_When_QueryStringProvided()
    {
        var matchingCorrelationId = "corr-filter-api-001";
        var first = await _driver.CreateAndCompleteDepositAsync("dep-integration-filter-101", matchingCorrelationId);
        await _driver.CreateAndCompleteDepositAsync("dep-integration-filter-102", "corr-filter-api-002");

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<DepositSummaryResponse>>(
            $"/api/v1/deposits?correlationId={matchingCorrelationId}&pageNumber=1&pageSize=20");

        response.Should().NotBeNull();
        response!.Items.Should().ContainSingle(item => item.TransactionId == first.TransactionId);
    }

    [Fact]
    public async Task GetAll_Should_FilterByCustomerId_And_AccountId_When_QueryStringProvided()
    {
        var created = await _driver.CreateAndCompleteDepositAsync("dep-integration-filter-201", "corr-filter-api-201");

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<DepositSummaryResponse>>(
            $"/api/v1/deposits?customerId={created.CustomerId}&accountId={created.AccountId}&pageNumber=1&pageSize=20");

        response.Should().NotBeNull();
        response!.Items.Should().Contain(item => item.TransactionId == created.TransactionId);
        response.Items.Should().OnlyContain(item => item.CustomerId == created.CustomerId && item.AccountId == created.AccountId);
        response.Items.Should().Contain(item => item.TransactionId == created.TransactionId && item.AccountNumber == "6222200000000000001");
    }

    [Fact]
    public async Task ResolvePendingReview_Should_ReturnConflictProblemDetails_When_DepositIsNotPendingReview()
    {
        var completed = await _driver.CreateAndCompleteDepositAsync($"dep-integration-invalid-review-{Guid.NewGuid():N}", "corr-invalid-review");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/deposits/{completed.TransactionId}/review/resolve",
            new ResolveDepositReviewRequest(
                DepositReviewResolution.ReversedExternally,
                "ops-user",
                "attempt invalid review resolution"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Deposit review action is invalid");
        problem.Detail.Should().Contain("Only pending review deposits can be resolved");
    }

    [Fact]
    public async Task GetAll_Should_ReturnEmptyItems_When_PageIsOutOfRange()
    {
        await _driver.CreateAndCompleteDepositAsync($"dep-integration-page-{Guid.NewGuid():N}", "corr-page-test");

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<DepositSummaryResponse>>(
            "/api/v1/deposits?pageNumber=2&pageSize=20");

        response.Should().NotBeNull();
        response!.TotalCount.Should().BeGreaterThan(0);
        response.Items.Should().BeEmpty();
    }
}
