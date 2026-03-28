using System.Net;
using System.Net.Http.Json;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Banking.Services.Deposit.Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Services.Deposit.IntegrationTests;

public sealed class DepositsApiTests : IClassFixture<DepositServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly DepositServiceWebApplicationFactory _factory;

    public DepositsApiTests(DepositServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostDeposits_Should_ReturnAccepted_When_RequestIsValid()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 500m, "CNY", DepositChannel.Counter, null, null);
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
            : await WaitForDepositAsync(deposit.TransactionId, DepositStatus.Succeeded);
        completed.Status.Should().Be(DepositStatus.Succeeded);
        completed.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PostDeposits_Should_ReturnConflict_When_AccountIsFrozen()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_frozen_001", 500m, "CNY", DepositChannel.Counter, null, null);
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deposits")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", "dep-integration-002");

        var response = await _client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ResolvePendingReview_Should_ReturnOk_When_DepositIsManuallyResolved()
    {
        var transactionId = "dep-review-api-001";
        await SeedPendingReviewDepositAsync(transactionId);

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
        await SeedPendingReviewDepositAsync(transactionId);

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<PendingReviewDepositSummaryResponse>>(
            "/api/v1/deposits/review/pending?pageNumber=1&pageSize=20");

        response.Should().NotBeNull();
        response!.Items.Should().ContainSingle(item => item.TransactionId == transactionId);
    }

    [Fact]
    public async Task GetAll_Should_FilterByStatus_When_QueryStringProvided()
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 320m, "CNY", DepositChannel.Counter, null, null);
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
            : await WaitForDepositAsync(created.TransactionId, DepositStatus.Succeeded);

        var response = await _client.GetFromJsonAsync<Banking.BuildingBlocks.Contracts.PagedResponse<DepositSummaryResponse>>(
            "/api/v1/deposits?status=Succeeded&pageNumber=1&pageSize=20");

        response.Should().NotBeNull();
        response!.Items.Should().Contain(item => item.TransactionId == completed.TransactionId);
        response.Items.Should().OnlyContain(item => item.Status == DepositStatus.Succeeded);
    }

    private async Task<DepositResponse> WaitForDepositAsync(string transactionId, DepositStatus expectedStatus)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await _client.GetFromJsonAsync<DepositResponse>($"/api/v1/deposits/{transactionId}");
            if (response is not null && response.Status == expectedStatus)
            {
                return response;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Deposit {transactionId} did not reach status {expectedStatus} in time.");
    }

    private async Task SeedPendingReviewDepositAsync(string transactionId)
    {
        using var scope = _factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IDepositRepository>();
        var transaction = new DepositTransaction
        {
            TransactionId = transactionId,
            TransactionNumber = $"D{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
            CustomerId = "cus_active_001",
            AccountId = "acc_active_001",
            Amount = 100m,
            Currency = "CNY",
            Channel = DepositChannel.Counter,
            Status = DepositStatus.PendingReview,
            AccountPostingStatus = DepositSagaStepStatus.Succeeded,
            AuditStatus = DepositSagaStepStatus.NotStarted,
            CompensationStatus = DepositSagaStepStatus.Failed,
            ReviewRequiredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            IdempotencyKey = $"idem-{transactionId}",
            CorrelationId = $"corr-{transactionId}",
            RequestedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        var outboxMessage = DepositOutboxMessage.CreateRequestedMessage(
            new DepositRequestedMessage(
                transaction.TransactionId,
                transaction.CustomerId,
                transaction.AccountId,
                transaction.Amount,
                transaction.Currency,
                transaction.Channel,
                transaction.CorrelationId),
            transaction.RequestedAt);

        await repository.AddAsync(transaction, outboxMessage, CancellationToken.None);
        await repository.MarkOutboxMessageProcessedAsync(outboxMessage.MessageId, DateTimeOffset.UtcNow, CancellationToken.None);
    }
}
