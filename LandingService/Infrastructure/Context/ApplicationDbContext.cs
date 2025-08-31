using Common;
using LandingService.Domain;
using Microsoft.EntityFrameworkCore;

namespace LandingService.Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (
            var entity in modelBuilder
                .Model.GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
        )
        {
            modelBuilder.Entity(entity.Name).Property<byte[]>("RowVersion").IsRowVersion();
            modelBuilder
                .Entity(entity.Name)
                .Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("GETUTCDATE()") // valor lo pone SQL Server
                .ValueGeneratedOnAdd();

            // UpdatedAt   (nullable, sin default; solo la declaramos)
            modelBuilder.Entity(entity.Name).Property<DateTime?>("UpdatedAt");

            // DeleteAt/DeletedAt (opcional, por coherencia)
            modelBuilder.Entity(entity.Name).Property<DateTime?>("DeleteAt");
        }

        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Session>().ToTable("Sessions");

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Session>(entity =>
        {
            entity.Property(e => e.TokenRequest).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Device).HasMaxLength(500);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.Latitude).HasMaxLength(20);
            entity.Property(e => e.Longitude).HasMaxLength(20);
        });

        modelBuilder
            .Entity<Session>()
            .HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
