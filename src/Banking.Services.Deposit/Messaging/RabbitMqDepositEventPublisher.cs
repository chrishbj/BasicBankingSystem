using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Banking.Services.Deposit.Messaging;

public sealed class RabbitMqDepositEventPublisher(IOptions<RabbitMqOptions> options) : IDepositEventPublisher
{
    public async Task PublishAsync(DepositRequestedMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var factory = new ConnectionFactory
        {
            Uri = new Uri(settings.ConnectionString)
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await DeclareTopologyAsync(channel, settings, cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            settings.ExchangeName,
            settings.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    internal static async Task DeclareTopologyAsync(
        IChannel channel,
        RabbitMqOptions settings,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            settings.ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            settings.QueueName,
            settings.ExchangeName,
            settings.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
