using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Account.Data;

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Account> Accounts => Set<Domain.Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<Domain.Account>();

        account.ToTable("accounts");
        account.HasKey(x => x.AccountId);

        account.Property(x => x.AccountId).HasMaxLength(64);
        account.Property(x => x.AccountNumber).HasMaxLength(32);
        account.Property(x => x.CustomerId).HasMaxLength(64);
        account.Property(x => x.AccountType).HasMaxLength(50);
        account.Property(x => x.Currency).HasMaxLength(3);
        account.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        account.Property(x => x.AvailableBalance).HasPrecision(18, 2);
        account.Property(x => x.LedgerBalance).HasPrecision(18, 2);

        account.HasIndex(x => x.AccountNumber).IsUnique();
        account.HasIndex(x => x.CustomerId);
    }
}
