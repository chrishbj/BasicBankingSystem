namespace Banking.Services.Deposit.Messaging;

public interface IDepositEventPublisher
{
    Task PublishAsync(DepositRequestedMessage message, CancellationToken cancellationToken);
}
