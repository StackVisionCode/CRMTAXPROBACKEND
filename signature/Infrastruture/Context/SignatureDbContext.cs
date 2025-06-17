using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;
public class SignatureDbContext : DbContext
{
  

    public SignatureDbContext(DbContextOptions<SignatureDbContext> options) : base(options) { }

      public DbSet<Signature> Signatures { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Signature>()
            .Property(s => s.Status)
            .HasConversion<string>(); // Guarda el enum como texto

        base.OnModelCreating(modelBuilder);
    }
}
