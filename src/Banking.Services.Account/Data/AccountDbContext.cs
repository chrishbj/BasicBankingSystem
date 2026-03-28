using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Account.Data;

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Account> Accounts => Set<Domain.Account>();
    public DbSet<Domain.AccountPosting> AccountPostings => Set<Domain.AccountPosting>();

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

        var posting = modelBuilder.Entity<Domain.AccountPosting>();

        posting.ToTable("account_postings");
        posting.HasKey(x => x.PostingReference);
        posting.Property(x => x.PostingReference).HasMaxLength(64);
        posting.Property(x => x.AccountId).HasMaxLength(64);
        posting.Property(x => x.PostingType).HasConversion<string>().HasMaxLength(30);
        posting.Property(x => x.Amount).HasPrecision(18, 2);
        posting.Property(x => x.Currency).HasMaxLength(3);
        posting.Property(x => x.CorrelationId).HasMaxLength(128);
        posting.Property(x => x.ReversalOfPostingReference).HasMaxLength(64);

        posting.HasIndex(x => x.AccountId);
        posting.HasIndex(x => x.ReversalOfPostingReference);
    }
}
