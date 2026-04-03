using System.Net.Http.Json;
using Banking.Services.Deposit.Contracts;
using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;
using Banking.Services.Deposit.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Services.Deposit.IntegrationTests.Support;

internal sealed class DepositApiDriver(HttpClient client, DepositServiceWebApplicationFactory factory)
{
    public async Task<DepositResponse> WaitForDepositAsync(string transactionId, DepositStatus expectedStatus)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await client.GetFromJsonAsync<DepositResponse>($"/api/v1/deposits/{transactionId}");
            if (response is not null && response.Status == expectedStatus)
            {
                return response;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Deposit {transactionId} did not reach status {expectedStatus} in time.");
    }

    public async Task<DepositResponse> CreateAndCompleteDepositAsync(string idempotencyKey, string? correlationId)
    {
        var request = new CreateDepositRequest("cus_active_001", "acc_active_001", 210m, "USD", DepositChannel.Counter, null, null);
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deposits")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            message.Headers.Add("X-Correlation-Id", correlationId);
        }

        var createResponse = await client.SendAsync(message);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<DepositResponse>()
            ?? throw new InvalidOperationException("Deposit creation response body was missing.");

        return created.Status == DepositStatus.Succeeded
            ? created
            : await WaitForDepositAsync(created.TransactionId, DepositStatus.Succeeded);
    }

    public async Task SeedPendingReviewDepositAsync(string transactionId, DateTimeOffset? requestedAt = null)
    {
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IDepositRepository>();
        var effectiveRequestedAt = requestedAt ?? DateTimeOffset.UtcNow.AddMinutes(-10);
        var transaction = new DepositTransaction
        {
            TransactionId = transactionId,
            TransactionNumber = $"D{effectiveRequestedAt:yyyyMMddHHmmssfff}",
            CustomerId = "cus_active_001",
            AccountId = "acc_active_001",
            Amount = 100m,
            Currency = "USD",
            Channel = DepositChannel.Counter,
            Status = DepositStatus.PendingReview,
            AccountPostingStatus = DepositSagaStepStatus.Succeeded,
            AuditStatus = DepositSagaStepStatus.NotStarted,
            CompensationStatus = DepositSagaStepStatus.Failed,
            ReviewRequiredAt = effectiveRequestedAt.AddMinutes(5),
            IdempotencyKey = $"idem-{transactionId}",
            CorrelationId = $"corr-{transactionId}",
            RequestedAt = effectiveRequestedAt
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
