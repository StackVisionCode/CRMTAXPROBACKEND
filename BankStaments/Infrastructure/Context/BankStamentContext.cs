using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class BankStamentContext : DbContext
{
    public BankStamentContext(DbContextOptions<BankStamentContext> options) : base(options)
    {
    }

  public DbSet<BankStatement> BankStatements { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

          modelBuilder.Entity<BankStatement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ClosingBalance).HasColumnType("decimal(18,2)");
                
                entity.HasMany(e => e.Transactions)
                    .WithOne(t => t.BankStatement)
                    .HasForeignKey(t => t.BankStatementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Category).HasMaxLength(50);
            });
    
        base.OnModelCreating(modelBuilder);
    }
}