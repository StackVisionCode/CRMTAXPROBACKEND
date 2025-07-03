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

            // *** INICIO DE LA CORRECCIÓN ***
            // Especificar explícitamente que SignatureImage debe ser de longitud máxima.
            // Esto se traduce a NVARCHAR(MAX) en SQL Server.
            b.Property(x => x.SignatureImage).HasColumnType("nvarchar(max)");
            // *** FIN DE LA CORRECCIÓN ***

            // Value-object mapeado en columnas propias
            b.OwnsOne(
                x => x.Certificate,
                c =>
                {
                    c.Property(p => p.Thumbprint).HasColumnName("CertThumbprint").HasMaxLength(64);
                    c.Property(p => p.Subject).HasColumnName("CertSubject").HasMaxLength(256);
                    c.Property(p => p.NotBefore).HasColumnName("CertNotBefore");
                    c.Property(p => p.NotAfter).HasColumnName("CertNotAfter");
                }
            );

            // He movido las propiedades de posición fuera del OwnsOne del certificado,
            // ya que parecen pertenecer directamente al Signer.
            b.Property(x => x.PositionX).HasColumnType("float");
            b.Property(x => x.PositionY).HasColumnType("float");

            // Value-object mapeado en columnas propias
            b.OwnsOne(
                I => I.InitialEntity,
                d =>
                {
                    d.Property(p => p.InitalValue).HasColumnName("InitialValue").HasMaxLength(4);
                    d.Property(p => p.WidthIntial)
                        .HasColumnName("WidthIntial")
                        .HasColumnType("float");
                    d.Property(p => p.HeightIntial)
                        .HasColumnName("HeightIntial")
                        .HasColumnType("float");
                    d.Property(p => p.PositionXIntial)
                        .HasColumnName("PositionXIntial")
                        .HasColumnType("float");
                    d.Property(p => p.PositionYIntial)
                        .HasColumnName("PositionYIntial")
                        .HasColumnType("float");
                }
            );

            // Value-object mapeado en columnas propias
            b.OwnsOne(
                f => f.FechaSigner,
                e =>
                {
                    e.Property(p => p.FechaValue).HasColumnName("FechaValue");
                    e.Property(p => p.WidthFechaSigner)
                        .HasColumnName("WidthFechaSigner")
                        .HasColumnType("float");
                    e.Property(p => p.HeightFechaSigner)
                        .HasColumnName("HeightFechaSigner")
                        .HasColumnType("float");
                    e.Property(p => p.PositionXFechaSigner)
                        .HasColumnName("PositionXFechaSigner")
                        .HasColumnType("float");
                    e.Property(p => p.PositionYFechaSigner)
                        .HasColumnName("PositionYFechaSigner")
                        .HasColumnType("float");
                }
            );
        });
    }
}
