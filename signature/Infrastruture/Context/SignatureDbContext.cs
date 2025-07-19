using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

/// <summary>
///  DbContext principal del micro-servicio «signature»
/// </summary>
public class SignatureDbContext : DbContext
{
    public SignatureDbContext(DbContextOptions<SignatureDbContext> o)
        : base(o) { }

    public DbSet<SignatureRequest> SignatureRequests => Set<SignatureRequest>();
    public DbSet<Signer> Signers => Set<Signer>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        /* ─────────────── SignatureRequest ─────────────── */
        mb.Entity<SignatureRequest>(builder =>
        {
            builder.ToTable("SignatureRequests");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(15);
            builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            builder.Property(x => x.RejectReason).HasMaxLength(500);
        });

        /* ──────────────────── Signer ──────────────────── */
        var signer = mb.Entity<Signer>();
        signer.ToTable("Signers");
        signer.HasKey(x => x.Id);
        signer.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);
        signer.Property(x => x.CustomerId).IsRequired(false);
        signer.Property(x => x.SignatureImage).HasColumnType("varchar(max)");
        signer.Property(x => x.RejectReason).HasMaxLength(500);
        signer.Property(x => x.FullName).HasMaxLength(150);

        /* value-object DigitalCertificate (propiedades sueltas) */
        signer.OwnsOne(
            s => s.Certificate,
            cert =>
            {
                cert.Property(p => p.Thumbprint).HasColumnName("CertThumbprint").HasMaxLength(64);
                cert.Property(p => p.Subject).HasColumnName("CertSubject").HasMaxLength(256);
                cert.Property(p => p.NotBefore).HasColumnName("CertNotBefore");
                cert.Property(p => p.NotAfter).HasColumnName("CertNotAfter");
            }
        );

        /* ----- SignatureBox (OwnsMany) ------------------ */
        signer.OwnsMany(
            s => s.Boxes,
            box =>
            {
                box.ToTable("SignatureBoxes");
                box.WithOwner().HasForeignKey("SignerId");

                box.HasKey(b => b.Id);
                box.Property(b => b.Id).HasColumnName("Id").ValueGeneratedNever();

                box.Property(p => p.PageNumber);
                box.Property(p => p.PositionX).HasColumnType("float");
                box.Property(p => p.PositionY).HasColumnType("float");
                box.Property(p => p.Width).HasColumnType("float");
                box.Property(p => p.Height).HasColumnType("float");

                /* ------ InitialEntity dentro de la caja ------ */
                box.OwnsOne(
                    p => p.InitialEntity,
                    ie =>
                    {
                        ie.Property(q => q.InitalValue)
                            .HasColumnName("InitialValue")
                            .HasMaxLength(4);
                        ie.Property(q => q.WidthIntial)
                            .HasColumnName("WidthIntial")
                            .HasColumnType("float");
                        ie.Property(q => q.HeightIntial)
                            .HasColumnName("HeightIntial")
                            .HasColumnType("float");
                        ie.Property(q => q.PositionXIntial)
                            .HasColumnName("PositionXIntial")
                            .HasColumnType("float");
                        ie.Property(q => q.PositionYIntial)
                            .HasColumnName("PositionYIntial")
                            .HasColumnType("float");
                    }
                );

                /* ------ FechaSigner dentro de la caja -------- */
                box.OwnsOne(
                    p => p.FechaSigner,
                    fs =>
                    {
                        fs.Property(q => q.FechaValue).HasColumnName("FechaValue");
                        fs.Property(q => q.WidthFechaSigner)
                            .HasColumnName("WidthFechaSigner")
                            .HasColumnType("float");
                        fs.Property(q => q.HeightFechaSigner)
                            .HasColumnName("HeightFechaSigner")
                            .HasColumnType("float");
                        fs.Property(q => q.PositionXFechaSigner)
                            .HasColumnName("PositionXFechaSigner")
                            .HasColumnType("float");
                        fs.Property(q => q.PositionYFechaSigner)
                            .HasColumnName("PositionYFechaSigner")
                            .HasColumnType("float");
                    }
                );
            }
        );
    }
}
