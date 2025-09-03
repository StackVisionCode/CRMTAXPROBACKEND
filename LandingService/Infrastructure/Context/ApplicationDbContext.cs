using Common;
using LandingService.Domain;
using Microsoft.EntityFrameworkCore;

namespace LandingService.Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Event> Events { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Speaker> Speakers { get; set; }
    public DbSet<Key> EventKeys { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes()
         .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entity.Name).Property<byte[]>("RowVersion").IsRowVersion();
            modelBuilder.Entity(entity.Name).Property<DateTime>("CreatedAt")
             .HasDefaultValueSql("GETUTCDATE()");// valor lo pone SQL Server .ValueGeneratedOnAdd(); 
             // UpdatedAt (nullable, sin default; solo la declaramos) 
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
        modelBuilder.Entity<Session>().HasOne(s => s.User)
        .WithMany(u => u.Sessions).HasForeignKey(s => s.UserId)
         .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Speaker>().ToTable("Speakers");
        modelBuilder.Entity<Speaker>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(150).IsRequired();



            entity.HasOne(s => s.Event)
                  .WithMany(e => e.Speakers)
                  .HasForeignKey(s => s.EventId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Key>().ToTable("EventKeys");
        modelBuilder.Entity<Key>(entity =>
        {
            entity.Property(k => k.Name).HasMaxLength(200).IsRequired();

            entity.HasOne(k => k.Event)
                  .WithMany(e => e.EventKeys)
                  .HasForeignKey(k => k.EventId)
                  .OnDelete(DeleteBehavior.NoAction);
        });



    }
}
