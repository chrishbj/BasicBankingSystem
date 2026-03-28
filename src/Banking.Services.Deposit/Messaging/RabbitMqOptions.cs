namespace Banking.Services.Deposit.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "Infrastructure:RabbitMq";

    public string Transport { get; init; } = DepositMessageTransport.RabbitMq;
    public string ConnectionString { get; init; } = "amqp://guest:guest@localhost:5672";
    public string ExchangeName { get; init; } = "basic-banking.deposit";
    public string QueueName { get; init; } = "basic-banking.deposit.requested";
    public string RoutingKey { get; init; } = "deposit.requested";
    public int PollingIntervalMilliseconds { get; init; } = 500;
}
