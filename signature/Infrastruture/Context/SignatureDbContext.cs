using Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class SignatureDbContext : DbContext
{
    public SignatureDbContext(DbContextOptions<SignatureDbContext> o)
        : base(o) { }

    public DbSet<SignatureRequest> SignatureRequests => Set<SignatureRequest>();
    public DbSet<Signer> Signers => Set<Signer>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<SignatureRequest>(b =>
        {
            b.ToTable("SignatureRequests");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(15);
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        mb.Entity<Signer>(b =>
        {
            b.ToTable("Signers");
            b.HasKey(x => x.Id);

            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);

            // Value-object mapeado en columnas propias
            b.OwnsOne(
                x => x.Certificate,
                c =>
                {
                    c.Property(p => p.Thumbprint).HasColumnName("CertThumbprint").HasMaxLength(64);
                    c.Property(p => p.Subject).HasColumnName("CertSubject").HasMaxLength(256);
                    c.Property(p => p.NotBefore).HasColumnName("CertNotBefore");
                    c.Property(p => p.NotAfter).HasColumnName("CertNotAfter");
                    b.Property(x => x.PositionX).HasColumnType("float");
                    b.Property(x => x.PositionY).HasColumnType("float");
                }
            );
        });
    }
}
