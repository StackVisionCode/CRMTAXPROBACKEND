using Domain;
using EmailServices.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Context;

public class EmailContext : DbContext
{
    public EmailContext(DbContextOptions<EmailContext> options)
        : base(options) { }

    //mapping table

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

        cfg.Property(c => c.Name).IsRequired().HasMaxLength(120);

        cfg.Property(c => c.ProviderType).IsRequired().HasMaxLength(10);

        cfg.Property(c => c.SmtpServer).HasMaxLength(150);
        cfg.Property(c => c.SmtpUsername).HasMaxLength(150);
        cfg.Property(c => c.SmtpPassword).HasMaxLength(150);
        cfg.Property(c => c.GmailClientId).HasMaxLength(200);
        cfg.Property(c => c.GmailEmailAddress).HasMaxLength(150);

        cfg.Property(c => c.DailyLimit).HasDefaultValue(100);

        // índice para búsquedas por usuario dueño
        cfg.HasIndex(c => c.UserId);

        /* ---------- Email ---------- */
        var email = modelBuilder.Entity<Email>();

        email.ToTable("Emails");
        email.HasKey(e => e.Id);

        // relación
        email
            .HasOne<EmailConfig>()
            .WithMany()
            .HasForeignKey(e => e.ConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        // enum EmailStatus → string
        var statusConverter = new EnumToStringConverter<EmailStatus>();
        email.Property(e => e.Status).HasConversion(statusConverter).HasMaxLength(10);

        email.Property(e => e.ToAddresses).IsRequired().HasMaxLength(500);
        email.Property(e => e.Subject).IsRequired().HasMaxLength(300);
        email.Property(e => e.Body).IsRequired();

        email.Property(e => e.CreatedOn).HasDefaultValueSql("GETUTCDATE()");

        email.HasIndex(e => e.ConfigId);
        email.HasIndex(e => e.SentOn);
        email.HasIndex(e => new { e.Status, e.CreatedOn });

        /* ---------- IncomingEmail ---------- */
        var incomingEmail = modelBuilder.Entity<IncomingEmail>();
        incomingEmail.ToTable("IncomingEmails");
        incomingEmail.HasKey(e => e.Id);
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

        incomingEmail.HasIndex(e => e.ConfigId);
        incomingEmail.HasIndex(e => e.ReceivedOn);
        incomingEmail.HasIndex(e => e.IsRead);

        // ⭐ CLAVE: ÍNDICE ÚNICO PARA PREVENIR DUPLICADOS ⭐
        incomingEmail
            .HasIndex(e => new { e.MessageId, e.ConfigId })
            .IsUnique()
            .HasDatabaseName("IX_IncomingEmails_MessageId_ConfigId_Unique");

        // Índice adicional para MessageId solo (para búsquedas rápidas)
        incomingEmail.HasIndex(e => e.MessageId).HasDatabaseName("IX_IncomingEmails_MessageId");

        /* ---------- EmailAttachment ---------- */
        var attachment = modelBuilder.Entity<EmailAttachment>();
        attachment.ToTable("EmailAttachments");
        attachment.HasKey(a => a.Id);
        attachment.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        attachment.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
        attachment.Property(a => a.FilePath).HasMaxLength(500);
        attachment.Property(a => a.CreatedOn).HasDefaultValueSql("GETUTCDATE()");
        attachment.HasIndex(a => a.EmailId);

        /* ---------- EmailTemplate ---------- */
        var template = modelBuilder.Entity<EmailTemplate>();
        template.ToTable("EmailTemplates");
        template.HasKey(t => t.Id);
        template.Property(t => t.Name).IsRequired().HasMaxLength(120);
        template.Property(t => t.Subject).IsRequired().HasMaxLength(300);
        template.Property(t => t.BodyTemplate).IsRequired();
        template.Property(t => t.Description).HasMaxLength(500);
        template.Property(t => t.TemplateVariables).HasMaxLength(2000);
        template.Property(t => t.CreatedOn).HasDefaultValueSql("GETUTCDATE()");
        template.HasIndex(t => t.UserId);
        template.HasIndex(t => t.IsActive);

        base.OnModelCreating(modelBuilder);
    }
}
