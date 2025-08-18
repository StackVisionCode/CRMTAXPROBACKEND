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
    public DbSet<SignatureBox> SignatureBoxes => Set<SignatureBox>();
    public DbSet<Signer> Signers => Set<Signer>();
    public DbSet<SignPreviewDocument> SignPreviewDocuments => Set<SignPreviewDocument>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        /* ─────────────── SignatureRequest ─────────────── */
        mb.Entity<SignatureRequest>(builder =>
        {
            builder.ToTable("SignatureRequests");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.CreatedByTaxUserId).IsRequired();

            // Índices para performance
            builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_SignatureRequests_CompanyId");

            builder
                .HasIndex(x => x.CreatedByTaxUserId)
                .HasDatabaseName("IX_SignatureRequests_CreatedByTaxUserId");

            builder
                .HasIndex(x => new { x.CompanyId, x.Status })
                .HasDatabaseName("IX_SignatureRequests_CompanyId_Status");

            builder.Property(x => x.LastModifiedByTaxUserId).IsRequired(false);
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

        // Relación 1-N con SignatureBox (ya no es OwnsMany)
        signer
            .HasMany(s => s.Boxes)
            .WithOne(b => b.Signer)
            .HasForeignKey(b => b.SignerId)
            .OnDelete(DeleteBehavior.Cascade);

        signer
            .HasOne(s => s.SignatureRequest)
            .WithMany(sr => sr.Signers)
            .HasForeignKey(s => s.SignatureRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        /* ─────────────── SignatureBox (Entidad Independiente) ─────────────── */
        var signatureBox = mb.Entity<SignatureBox>();
        signatureBox.ToTable("SignatureBoxes");
        signatureBox.HasKey(x => x.Id);

        // Propiedades escalares
        signatureBox.Property(p => p.SignerId).IsRequired();
        signatureBox.Property(p => p.PageNumber).IsRequired();
        signatureBox.Property(p => p.PositionX).HasColumnType("float").IsRequired();
        signatureBox.Property(p => p.PositionY).HasColumnType("float").IsRequired();
        signatureBox.Property(p => p.Width).HasColumnType("float").IsRequired();
        signatureBox.Property(p => p.Height).HasColumnType("float").IsRequired();
        signatureBox.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();

        /* ------ InitialEntity dentro de la caja (OwnsOne) ------ */
        signatureBox.OwnsOne(
            p => p.InitialEntity,
            ie =>
            {
                ie.Property(q => q.InitalValue).HasColumnName("InitialValue").HasMaxLength(4);
                ie.Property(q => q.WidthIntial).HasColumnName("WidthIntial").HasColumnType("float");
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

        /* ------ FechaSigner dentro de la caja (OwnsOne) -------- */
        signatureBox.OwnsOne(
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

        // Índices para mejorar rendimiento
        signatureBox.HasIndex(x => x.SignerId);
        signatureBox.HasIndex(x => new { x.SignerId, x.PageNumber });

        /* ─────────────── SignPreviewDocument ─────────────── */
        var signpreviewdocument = mb.Entity<SignPreviewDocument>();
        signpreviewdocument.ToTable("SignPreviewDocuments"); // ← Nombre correcto de tabla
        signpreviewdocument.HasKey(dp => dp.Id);

        // Propiedades con restricciones
        signpreviewdocument.Property(dp => dp.AccessToken).IsRequired().HasMaxLength(100);
        signpreviewdocument.Property(dp => dp.SessionId).IsRequired().HasMaxLength(100);
        signpreviewdocument.Property(dp => dp.RequestFingerprint).IsRequired().HasMaxLength(50);
        signpreviewdocument.Property(dp => dp.LastAccessIp).HasMaxLength(45);
        signpreviewdocument.Property(dp => dp.LastAccessUserAgent).HasMaxLength(500);
        signpreviewdocument.Property(dp => dp.SignatureRequestId).IsRequired();
        signpreviewdocument.Property(dp => dp.SignerId).IsRequired();
        signpreviewdocument.Property(dp => dp.OriginalDocumentId).IsRequired();
        signpreviewdocument.Property(dp => dp.SealedDocumentId).IsRequired();
        signpreviewdocument.Property(dp => dp.ExpiresAt).IsRequired();
        signpreviewdocument.Property(dp => dp.IsActive).IsRequired();
        signpreviewdocument.Property(dp => dp.AccessCount).IsRequired();
        signpreviewdocument.Property(dp => dp.MaxAccessCount).IsRequired();

        // Índices para optimizar consultas
        signpreviewdocument
            .HasIndex(dp => new { dp.AccessToken, dp.SessionId })
            .IsUnique()
            .HasDatabaseName("IX_SignPreviewDocuments_AccessToken_SessionId");

        signpreviewdocument
            .HasIndex(dp => dp.SignatureRequestId)
            .HasDatabaseName("IX_SignPreviewDocuments_SignatureRequestId");

        signpreviewdocument
            .HasIndex(dp => dp.SignerId)
            .HasDatabaseName("IX_SignPreviewDocuments_SignerId");

        signpreviewdocument
            .HasIndex(dp => dp.ExpiresAt)
            .HasDatabaseName("IX_SignPreviewDocuments_ExpiresAt");

        signpreviewdocument
            .HasIndex(dp => new { dp.IsActive, dp.ExpiresAt })
            .HasDatabaseName("IX_SignPreviewDocuments_Active_Expires");

        signpreviewdocument
            .HasIndex(dp => dp.SealedDocumentId)
            .HasDatabaseName("IX_SignPreviewDocuments_SealedDocumentId");

        // Relaciones explícitas
        signpreviewdocument
            .HasOne(dp => dp.SignatureRequest)
            .WithMany() // Sin navegación inversa por simplicidad
            .HasForeignKey(dp => dp.SignatureRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        signpreviewdocument
            .HasOne(dp => dp.Signer)
            .WithMany() // Sin navegación inversa por simplicidad
            .HasForeignKey(dp => dp.SignerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
