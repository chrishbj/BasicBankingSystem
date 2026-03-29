using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Services.Deposit.Data;

public static class DepositSchemaUpgradeExtensions
{
    public static async Task EnsureDepositSchemaUpToDateAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DepositDbContext>();

        if (!dbContext.Database.IsRelational() || (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "AccountPostingStatus" character varying(30) NOT NULL DEFAULT 'NotStarted';

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "AuditStatus" character varying(30) NOT NULL DEFAULT 'NotStarted';

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "CompensationStatus" character varying(30) NOT NULL DEFAULT 'NotStarted';

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "ReviewResolution" character varying(30) NOT NULL DEFAULT 'None';

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "CompensationRetryCount" integer NOT NULL DEFAULT 0;

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "ReviewLastActionBy" character varying(100);

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "ReviewNote" character varying(1000);

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "ReviewRequiredAt" timestamp with time zone;

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "ReviewResolvedAt" timestamp with time zone;

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "LastCompensationAttemptAt" timestamp with time zone;

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "LastProcessedAt" timestamp with time zone;

            ALTER TABLE IF EXISTS deposit_transactions
            ADD COLUMN IF NOT EXISTS "ReferenceNumber" character varying(128);
            """,
            cancellationToken);
    }
}
