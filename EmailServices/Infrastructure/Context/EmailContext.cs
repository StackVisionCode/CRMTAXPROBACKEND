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

        base.OnModelCreating(modelBuilder);
    }
}
