using Domain;
using EmailServices.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Context;

public class EmailContext : DbContext
{
    public EmailContext(DbContextOptions<EmailContext> options)
        : base(options) { }

    // Mapping tables
    public DbSet<Email> Emails { get; set; }
    public DbSet<EmailConfig> EmailConfigs { get; set; }
    public DbSet<IncomingEmail> IncomingEmails { get; set; }
    public DbSet<EmailAttachment> EmailAttachments { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        /* ---------- EmailConfig ---------- */
        var cfg = modelBuilder.Entity<EmailConfig>();

        cfg.ToTable("EmailConfigs");
        cfg.HasKey(c => c.Id);

        // NUEVO: CompanyId como campo obligatorio
        cfg.Property(c => c.CompanyId).IsRequired();

        // NUEVO: Auditoría obligatoria
        cfg.Property(c => c.CreatedByTaxUserId).IsRequired();
        cfg.Property(c => c.LastModifiedByTaxUserId).IsRequired(false);

        cfg.Property(c => c.Name).IsRequired().HasMaxLength(120);
        cfg.Property(c => c.ProviderType).IsRequired().HasMaxLength(10);

        cfg.Property(c => c.SmtpServer).HasMaxLength(150);
        cfg.Property(c => c.SmtpUsername).HasMaxLength(150);
        cfg.Property(c => c.SmtpPassword).HasMaxLength(150);
        cfg.Property(c => c.GmailClientId).HasMaxLength(200);
        cfg.Property(c => c.GmailEmailAddress).HasMaxLength(150);

        cfg.Property(c => c.DailyLimit).HasDefaultValue(100);
        cfg.Property(c => c.CreatedOn).HasDefaultValueSql("GETUTCDATE()");

        // Índices para performance con CompanyId y auditoría
        cfg.HasIndex(c => c.CompanyId).HasDatabaseName("IX_EmailConfigs_CompanyId");

        cfg.HasIndex(c => c.CreatedByTaxUserId)
            .HasDatabaseName("IX_EmailConfigs_CreatedByTaxUserId");

        cfg.HasIndex(c => new { c.CompanyId, c.IsActive })
            .HasDatabaseName("IX_EmailConfigs_CompanyId_IsActive")
            .HasFilter("[IsActive] = 1");

        /* ---------- Email ---------- */
        var email = modelBuilder.Entity<Email>();

        email.ToTable("Emails");
        email.HasKey(e => e.Id);

        //CompanyId como campo obligatorio
        email.Property(e => e.CompanyId).IsRequired();

        // Auditoría obligatoria
        email.Property(e => e.CreatedByTaxUserId).IsRequired();
        email.Property(e => e.LastModifiedByTaxUserId).IsRequired(false);
        email.Property(e => e.SentByTaxUserId).IsRequired();

        // Relación con EmailConfig
        email
            .HasOne<EmailConfig>()
            .WithMany()
            .HasForeignKey(e => e.ConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enum EmailStatus → string
        var statusConverter = new EnumToStringConverter<EmailStatus>();
        email.Property(e => e.Status).HasConversion(statusConverter).HasMaxLength(10);

        email.Property(e => e.ToAddresses).IsRequired().HasMaxLength(500);
        email.Property(e => e.CcAddresses).HasMaxLength(500);
        email.Property(e => e.BccAddresses).HasMaxLength(500);
        email.Property(e => e.Subject).IsRequired().HasMaxLength(300);
        email.Property(e => e.Body).IsRequired();

        email.Property(e => e.CreatedOn).HasDefaultValueSql("GETUTCDATE()");

        // Índices para performance con CompanyId y auditoría
        email.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Emails_CompanyId");

        email.HasIndex(e => e.CreatedByTaxUserId).HasDatabaseName("IX_Emails_CreatedByTaxUserId");

        email.HasIndex(e => e.SentByTaxUserId).HasDatabaseName("IX_Emails_SentByTaxUserId");

        email
            .HasIndex(e => new
            {
                e.CompanyId,
                e.Status,
                e.CreatedOn,
            })
            .HasDatabaseName("IX_Emails_CompanyId_Status_CreatedOn");

        email
            .HasIndex(e => new { e.CompanyId, e.ConfigId })
            .HasDatabaseName("IX_Emails_CompanyId_ConfigId");

        // Índices existentes actualizados
        email.HasIndex(e => e.ConfigId);
        email.HasIndex(e => e.SentOn);
        email.HasIndex(e => new { e.Status, e.CreatedOn });

        /* ---------- IncomingEmail ---------- */
        var incomingEmail = modelBuilder.Entity<IncomingEmail>();

        incomingEmail.ToTable("IncomingEmails");
        incomingEmail.HasKey(e => e.Id);

        // CompanyId como campo obligatorio
        incomingEmail.Property(e => e.CompanyId).IsRequired();

        // Auditoría
        incomingEmail.Property(e => e.CreatedByTaxUserId).IsRequired();

        incomingEmail
            .HasOne<EmailConfig>()
            .WithMany()
            .HasForeignKey(e => e.ConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        incomingEmail.Property(e => e.FromAddress).IsRequired().HasMaxLength(200);
        incomingEmail.Property(e => e.ToAddress).IsRequired().HasMaxLength(200);
        incomingEmail.Property(e => e.CcAddresses).HasMaxLength(500);
        incomingEmail.Property(e => e.Subject).IsRequired().HasMaxLength(300);
        incomingEmail.Property(e => e.Body).IsRequired();
        incomingEmail.Property(e => e.MessageId).HasMaxLength(200);
        incomingEmail.Property(e => e.InReplyTo).HasMaxLength(200);
        incomingEmail.Property(e => e.References).HasMaxLength(1000);
        incomingEmail.Property(e => e.ReceivedOn).HasDefaultValueSql("GETUTCDATE()");

        // NUEVO: Índices para performance con CompanyId
        incomingEmail.HasIndex(e => e.CompanyId).HasDatabaseName("IX_IncomingEmails_CompanyId");

        incomingEmail
            .HasIndex(e => new { e.CompanyId, e.IsRead })
            .HasDatabaseName("IX_IncomingEmails_CompanyId_IsRead");

        incomingEmail
            .HasIndex(e => new { e.CompanyId, e.ReceivedOn })
            .HasDatabaseName("IX_IncomingEmails_CompanyId_ReceivedOn");

        // Índices existentes
        incomingEmail.HasIndex(e => e.ConfigId);
        incomingEmail.HasIndex(e => e.ReceivedOn);
        incomingEmail.HasIndex(e => e.IsRead);

        // ⭐ ÍNDICE ÚNICO ACTUALIZADO PARA PREVENIR DUPLICADOS ⭐
        incomingEmail
            .HasIndex(e => new
            {
                e.MessageId,
                e.ConfigId,
                e.CompanyId,
            })
            .IsUnique()
            .HasDatabaseName("IX_IncomingEmails_MessageId_ConfigId_CompanyId_Unique");

        // Índice adicional para MessageId
        incomingEmail.HasIndex(e => e.MessageId).HasDatabaseName("IX_IncomingEmails_MessageId");

        /* ---------- EmailAttachment ---------- */
        var attachment = modelBuilder.Entity<EmailAttachment>();

        attachment.ToTable("EmailAttachments");
        attachment.HasKey(a => a.Id);

        // CompanyId como campo obligatorio
        attachment.Property(a => a.CompanyId).IsRequired();

        attachment.Property(a => a.EmailId).IsRequired();
        attachment.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        attachment.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
        attachment.Property(a => a.FilePath).HasMaxLength(500);
        attachment.Property(a => a.CreatedOn).HasDefaultValueSql("GETUTCDATE()");

        // Índices para performance con CompanyId
        attachment.HasIndex(a => a.CompanyId).HasDatabaseName("IX_EmailAttachments_CompanyId");

        attachment
            .HasIndex(a => new { a.CompanyId, a.EmailId })
            .HasDatabaseName("IX_EmailAttachments_CompanyId_EmailId");

        // Índice existente
        attachment.HasIndex(a => a.EmailId);

        /* ---------- EmailTemplate ---------- */
        var template = modelBuilder.Entity<EmailTemplate>();

        template.ToTable("EmailTemplates");
        template.HasKey(t => t.Id);

        // CompanyId como campo obligatorio
        template.Property(t => t.CompanyId).IsRequired();

        // Auditoría obligatoria
        template.Property(t => t.CreatedByTaxUserId).IsRequired();
        template.Property(t => t.LastModifiedByTaxUserId).IsRequired(false);

        template.Property(t => t.Name).IsRequired().HasMaxLength(120);
        template.Property(t => t.Subject).IsRequired().HasMaxLength(300);
        template.Property(t => t.BodyTemplate).IsRequired();
        template.Property(t => t.Description).HasMaxLength(500);
        template.Property(t => t.TemplateVariables).HasMaxLength(2000);
        template.Property(t => t.CreatedOn).HasDefaultValueSql("GETUTCDATE()");

        // Índices para performance con CompanyId y auditoría
        template.HasIndex(t => t.CompanyId).HasDatabaseName("IX_EmailTemplates_CompanyId");

        template
            .HasIndex(t => t.CreatedByTaxUserId)
            .HasDatabaseName("IX_EmailTemplates_CreatedByTaxUserId");

        template
            .HasIndex(t => new { t.CompanyId, t.IsActive })
            .HasDatabaseName("IX_EmailTemplates_CompanyId_IsActive")
            .HasFilter("[IsActive] = 1");

        template
            .HasIndex(t => new { t.CompanyId, t.Name })
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_CompanyId_Name_Unique");

        // Índices existentes actualizados
        template.HasIndex(t => t.IsActive);

        /* ---------- Constraints Adicionales ---------- */

        // Constraint: EmailConfig debe tener configuración válida según el tipo
        cfg.ToTable(b =>
            b.HasCheckConstraint(
                "CK_EmailConfigs_SmtpConfig",
                "([ProviderType] != 'Smtp') OR ([SmtpServer] IS NOT NULL AND [SmtpPort] IS NOT NULL AND [SmtpUsername] IS NOT NULL)"
            )
        );

        cfg.ToTable(b =>
            b.HasCheckConstraint(
                "CK_EmailConfigs_GmailConfig",
                "([ProviderType] != 'Gmail') OR ([GmailClientId] IS NOT NULL AND [GmailEmailAddress] IS NOT NULL)"
            )
        );

        // Constraint: DailyLimit debe ser positivo
        cfg.ToTable(b => b.HasCheckConstraint("CK_EmailConfigs_DailyLimit", "[DailyLimit] > 0"));

        base.OnModelCreating(modelBuilder);
    }
}
