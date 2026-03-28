using System.Net;
using System.Net.Http.Json;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using FluentAssertions;

namespace Banking.Services.Deposit.IntegrationTests;

public sealed class DepositsApiTests : IClassFixture<DepositServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DepositsApiTests(DepositServiceWebApplicationFactory factory)
    {
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
        deposit!.Status.Should().Be(DepositStatus.Received);

        var completed = await WaitForDepositAsync(deposit.TransactionId, DepositStatus.Succeeded);
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
}
