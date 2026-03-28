using Banking.Services.Deposit.Services;

namespace Banking.Services.Deposit.Messaging;

public sealed class InMemoryDepositMessageConsumer(
    InMemoryDepositMessageQueue messageQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<InMemoryDepositMessageConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in messageQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IDepositTransactionProcessor>();
                await processor.ProcessAsync(message.TransactionId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to process in-memory deposit message for transaction {TransactionId}.",
                    message.TransactionId);
            }
        }
    }
}
