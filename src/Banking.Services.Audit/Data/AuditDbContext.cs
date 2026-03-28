using System.Text.Json;
using Banking.Services.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Banking.Services.Audit.Data;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dictionaryConverter = new ValueConverter<Dictionary<string, object?>?, string?>(
            value => value == null ? null : JsonSerializer.Serialize(value, JsonSerializerOptions.Web),
            value => string.IsNullOrWhiteSpace(value)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(value, JsonSerializerOptions.Web));

        var auditLog = modelBuilder.Entity<AuditLog>();

        auditLog.ToTable("audit_logs");
        auditLog.HasKey(x => x.AuditId);

        auditLog.Property(x => x.AuditId).HasMaxLength(64);
        auditLog.Property(x => x.ActorType).HasMaxLength(50);
        auditLog.Property(x => x.ActorId).HasMaxLength(100);
        auditLog.Property(x => x.Action).HasMaxLength(100);
        auditLog.Property(x => x.AggregateType).HasMaxLength(100);
        auditLog.Property(x => x.AggregateId).HasMaxLength(100);
        auditLog.Property(x => x.CorrelationId).HasMaxLength(128);
        auditLog.Property(x => x.CausationId).HasMaxLength(128);
        auditLog.Property(x => x.BeforeSnapshot).HasConversion(dictionaryConverter).HasColumnType("text");
        auditLog.Property(x => x.AfterSnapshot).HasConversion(dictionaryConverter).HasColumnType("text");

        auditLog.HasIndex(x => x.CorrelationId);
        auditLog.HasIndex(x => new { x.AggregateType, x.AggregateId });
    }
}
