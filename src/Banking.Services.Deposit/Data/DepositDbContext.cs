using Banking.Services.Deposit.Domain;
using Banking.Services.Deposit.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Deposit.Data;

public sealed class DepositDbContext(DbContextOptions<DepositDbContext> options) : DbContext(options)
{
    public DbSet<DepositTransaction> Deposits => Set<DepositTransaction>();
    public DbSet<DepositOutboxMessage> OutboxMessages => Set<DepositOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var deposit = modelBuilder.Entity<DepositTransaction>();

        deposit.ToTable("deposit_transactions");
        deposit.HasKey(x => x.TransactionId);
        deposit.Property(x => x.TransactionId).HasMaxLength(64);
        deposit.Property(x => x.TransactionNumber).HasMaxLength(32);
        deposit.Property(x => x.CustomerId).HasMaxLength(64);
        deposit.Property(x => x.AccountId).HasMaxLength(64);
        deposit.Property(x => x.Amount).HasPrecision(18, 2);
        deposit.Property(x => x.Currency).HasMaxLength(3);
        deposit.Property(x => x.ReferenceNumber).HasMaxLength(128);
        deposit.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
        deposit.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        deposit.Property(x => x.AccountPostingStatus).HasConversion<string>().HasMaxLength(30);
        deposit.Property(x => x.AuditStatus).HasConversion<string>().HasMaxLength(30);
        deposit.Property(x => x.CompensationStatus).HasConversion<string>().HasMaxLength(30);
        deposit.Property(x => x.ReviewResolution).HasConversion<string>().HasMaxLength(30);
        deposit.Property(x => x.IdempotencyKey).HasMaxLength(128);
        deposit.Property(x => x.CorrelationId).HasMaxLength(128);
        deposit.Property(x => x.FailureCode).HasMaxLength(64);
        deposit.Property(x => x.FailureReason).HasMaxLength(500);
        deposit.Property(x => x.ReviewLastActionBy).HasMaxLength(100);
        deposit.Property(x => x.ReviewNote).HasMaxLength(1000);

        // Unique transaction and idempotency keys protect both operator visibility and financial safety.
        deposit.HasIndex(x => x.TransactionNumber).IsUnique();
        deposit.HasIndex(x => x.IdempotencyKey).IsUnique();
        // PendingReview scanning is status-driven, so this index favors the retry worker and operator queue.
        deposit.HasIndex(x => new { x.Status, x.LastCompensationAttemptAt });

        var outbox = modelBuilder.Entity<DepositOutboxMessage>();

        outbox.ToTable("deposit_outbox_messages");
        outbox.HasKey(x => x.MessageId);
        outbox.Property(x => x.MessageId).HasMaxLength(64);
        outbox.Property(x => x.TransactionId).HasMaxLength(64);
        outbox.Property(x => x.MessageType).HasMaxLength(100);
        outbox.Property(x => x.Payload).HasColumnType("text");
        outbox.Property(x => x.LastError).HasMaxLength(1000);

        // The dispatcher reads unprocessed messages in occurred order, so the processed/occurred
        // index keeps outbox polling efficient even as the table grows.
        outbox.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
    }
}
