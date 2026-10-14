using CrdbLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrdbLab.Api.Data;

public class BankDbContext(DbContextOptions<BankDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(c => c.Id).HasName("pk_customers");

            e.Property(c => c.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            e.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.ToTable("transactions");
            e.HasKey(t => t.Id).HasName("pk_transactions");

            e.Property(t => t.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            e.Property(t => t.CustomerId).HasColumnName("customer_id");

            e.Property(t => t.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2);

            e.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()");

            e.HasOne(t => t.Customer)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CustomerId)
                .HasConstraintName("fk_transactions_customer")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(t => t.CustomerId).HasDatabaseName("ix_transactions_customer_id");
        });
    }
}
