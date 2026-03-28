using System.Text.Json;
using Banking.Services.Deposit.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Banking.Services.Deposit.Messaging;

public sealed class RabbitMqDepositMessageConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqDepositMessageConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "RabbitMQ deposit consumer loop failed. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var factory = new ConnectionFactory
        {
            Uri = new Uri(settings.ConnectionString)
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await RabbitMqDepositEventPublisher.DeclareTopologyAsync(channel, settings, cancellationToken);
        await channel.BasicQosAsync(0, 1, false, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var delivery = await channel.BasicGetAsync(settings.QueueName, autoAck: false, cancellationToken);
            if (delivery is null)
            {
                await Task.Delay(settings.PollingIntervalMilliseconds, cancellationToken);
                continue;
            }

            var message = JsonSerializer.Deserialize<DepositRequestedMessage>(delivery.Body.Span);
            if (message is null)
            {
                await channel.BasicAckAsync(delivery.DeliveryTag, false, cancellationToken);
                continue;
            }

            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IDepositTransactionProcessor>();
                await processor.ProcessAsync(message.TransactionId, cancellationToken);
                await channel.BasicAckAsync(delivery.DeliveryTag, false, cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(
                    exception,
                    "Failed to process RabbitMQ deposit message for transaction {TransactionId}.",
                    message.TransactionId);

                await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: true, cancellationToken: cancellationToken);
            }
        }
    }
}
