using Microsoft.EntityFrameworkCore;
using SMSServices.Domain.Entities;

namespace SMSServices.Infrastructure.Context;

public class SmsDbContext : DbContext
{
    public SmsDbContext(DbContextOptions<SmsDbContext> options) : base(options)
    {
    }

    public DbSet<SmsMessage> SmsMessages { get; set; }
    public DbSet<SmsTemplate> SmsTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de SmsMessage
        modelBuilder.Entity<SmsMessage>(entity =>
        {
            entity.ToTable("SmsMessages");
            
            entity.HasKey(e => e.Id);
            
            // Para Guid, usar ValueGeneratedOnAdd() en lugar de UseIdentityColumn()
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd(); // Esto genera un nuevo Guid automáticamente

            entity.Property(e => e.MessageSid)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.From)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.To)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Body)
                .IsRequired()
                .HasMaxLength(1600);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Direction)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.Price)
                .HasColumnType("decimal(10,4)");

            entity.Property(e => e.PriceUnit)
                .HasMaxLength(10);

            entity.Property(e => e.NumSegments)
                .HasMaxLength(10);

            entity.Property(e => e.NumMedia)
                .HasMaxLength(10);

            entity.Property(e => e.ErrorCode)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);

            entity.Property(e => e.AccountSid)
                .HasMaxLength(50);

            entity.Property(e => e.MessagingServiceSid)
                .HasMaxLength(50);

            // Índices para búsquedas comunes
            entity.HasIndex(e => e.MessageSid)
                .IsUnique()
                .HasDatabaseName("IX_SmsMessages_MessageSid");

            entity.HasIndex(e => e.From)
                .HasDatabaseName("IX_SmsMessages_From");

            entity.HasIndex(e => e.To)
                .HasDatabaseName("IX_SmsMessages_To");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_SmsMessages_CreatedAt");

            entity.HasIndex(e => e.Direction)
                .HasDatabaseName("IX_SmsMessages_Direction");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_SmsMessages_Status");
        });

        // Configuración de SmsTemplate
        modelBuilder.Entity<SmsTemplate>(entity =>
        {
            entity.ToTable("SmsTemplates");
            
            entity.HasKey(e => e.Id);
            
            // Para Guid, usar ValueGeneratedOnAdd() en lugar de UseIdentityColumn()
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Template)
                .IsRequired()
                .HasMaxLength(1600);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_SmsTemplates_Name");
        });

        // Datos semilla para plantillas - USANDO IDs FIJOS para evitar problemas
        modelBuilder.Entity<SmsTemplate>().HasData(
            new SmsTemplate
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Name = "Verification",
                Template = "Tu código de verificación es: {codigo}. Válido por 10 minutos.",
                Description = "Plantilla para códigos de verificación",
                CreatedBy = "System",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new SmsTemplate
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Name = "Welcome",
                Template = "¡Bienvenido {nombre}! Gracias por registrarte en nuestro servicio.",
                Description = "Mensaje de bienvenida",
                CreatedBy = "System",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new SmsTemplate
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Name = "OrderConfirmation",
                Template = "Tu pedido #{orderId} ha sido confirmado. Total: ${total}. Tiempo estimado: {tiempo}.",
                Description = "Confirmación de pedido",
                CreatedBy = "System",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
    }
}