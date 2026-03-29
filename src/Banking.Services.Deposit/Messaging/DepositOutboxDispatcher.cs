using System.Text.Json;
using Banking.Services.Deposit.Repositories;
using Microsoft.Extensions.Options;

namespace Banking.Services.Deposit.Messaging;

public sealed class DepositOutboxDispatcher(
    IServiceScopeFactory serviceScopeFactory,
    IDepositEventPublisher eventPublisher,
    IOptions<RabbitMqOptions> options,
    ILogger<DepositOutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(Math.Max(50, options.Value.PollingIntervalMilliseconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Polling the outbox keeps message publication decoupled from the request thread.
                var dispatchedCount = await DispatchPendingMessagesAsync(stoppingToken);
                if (dispatchedCount == 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Deposit outbox dispatch failed. Retrying.");
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    public async Task<int> DispatchPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDepositRepository>();
        var messages = await repository.GetPendingOutboxMessagesAsync(20, cancellationToken);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var depositRequested = JsonSerializer.Deserialize<DepositRequestedMessage>(message.Payload);
            if (depositRequested is null)
            {
                // Bad payloads are marked processed so the dispatcher does not poison the queue forever.
                await repository.MarkOutboxMessageProcessedAsync(message.MessageId, DateTimeOffset.UtcNow, cancellationToken);
                continue;
            }

            await eventPublisher.PublishAsync(depositRequested, cancellationToken);
            await repository.MarkOutboxMessageProcessedAsync(message.MessageId, DateTimeOffset.UtcNow, cancellationToken);
        }

        return messages.Count;
    }
}
