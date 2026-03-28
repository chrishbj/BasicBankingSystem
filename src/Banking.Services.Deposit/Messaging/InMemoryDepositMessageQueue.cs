using System.Threading.Channels;

namespace Banking.Services.Deposit.Messaging;

public sealed class InMemoryDepositMessageQueue : IDepositEventPublisher
{
    private readonly Channel<DepositRequestedMessage> _channel = Channel.CreateUnbounded<DepositRequestedMessage>();

    public Task PublishAsync(DepositRequestedMessage message, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken).AsTask();
    }

    public IAsyncEnumerable<DepositRequestedMessage> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
