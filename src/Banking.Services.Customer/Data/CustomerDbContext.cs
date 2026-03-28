using Microsoft.EntityFrameworkCore;

namespace Banking.Services.Customer.Data;

public sealed class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Customer> Customers => Set<Domain.Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<Domain.Customer>();

        customer.ToTable("customers");
        customer.HasKey(x => x.CustomerId);

        customer.Property(x => x.CustomerId).HasMaxLength(64);
        customer.Property(x => x.CustomerNumber).HasMaxLength(32);
        customer.Property(x => x.FullName).HasMaxLength(200);
        customer.Property(x => x.IdentityType).HasMaxLength(50);
        customer.Property(x => x.IdentityNumber).HasMaxLength(50);
        customer.Property(x => x.Mobile).HasMaxLength(30);
        customer.Property(x => x.Email).HasMaxLength(200);
        customer.Property(x => x.RiskLevel).HasMaxLength(30);
        customer.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        customer.HasIndex(x => x.CustomerNumber).IsUnique();
        customer.HasIndex(x => x.Mobile).IsUnique();
        customer.HasIndex(x => new { x.IdentityType, x.IdentityNumber }).IsUnique();

        customer.OwnsOne(x => x.Address, address =>
        {
            address.Property(x => x.Country).HasColumnName("country").HasMaxLength(2);
            address.Property(x => x.Province).HasColumnName("province").HasMaxLength(100);
            address.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            address.Property(x => x.Line1).HasColumnName("line1").HasMaxLength(200);
            address.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        });
    }
}
