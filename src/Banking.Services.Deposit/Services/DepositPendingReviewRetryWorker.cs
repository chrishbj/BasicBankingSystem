using Microsoft.Extensions.Options;

namespace Banking.Services.Deposit.Services;

public sealed class DepositPendingReviewRetryWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<PendingReviewRetryOptions> options,
    ILogger<DepositPendingReviewRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Automatic pending review retry is disabled.");
            return;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(250, settings.PollingIntervalMilliseconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var depositRepository = scope.ServiceProvider.GetRequiredService<Repositories.IDepositRepository>();
                var processor = scope.ServiceProvider.GetRequiredService<IDepositTransactionProcessor>();
                var candidates = await depositRepository.GetPendingReviewAsync(20, stoppingToken);

                foreach (var transaction in candidates.Where(CanRetryAutomatically))
                {
                    if (transaction.CompensationRetryCount >= settings.MaxAutomaticRetries)
                    {
                        continue;
                    }

                    await processor.RetryCompensationAsync(
                        transaction.TransactionId,
                        "system-auto-retry",
                        "Automatic compensation retry for pending review deposit.",
                        stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Automatic pending review retry cycle failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private static bool CanRetryAutomatically(Domain.DepositTransaction transaction)
    {
        return transaction.Status == Domain.DepositStatus.PendingReview &&
               transaction.CompensationStatus == Domain.DepositSagaStepStatus.Failed;
    }
}
