using CommLinkService.Domain.Entities;
using Common;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Infrastructure.Persistence;

public interface ICommLinkDbContext
{
    DbSet<Room> Rooms { get; }
    DbSet<RoomParticipant> RoomParticipants { get; }
    DbSet<Message> Messages { get; }
    DbSet<MessageReaction> MessageReactions { get; }
    DbSet<Connection> Connections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class CommLinkDbContext : DbContext, ICommLinkDbContext
{
    public CommLinkDbContext(DbContextOptions<CommLinkDbContext> options)
        : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomParticipant> RoomParticipants => Set<RoomParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<Connection> Connections => Set<Connection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplicar configuraciones desde assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommLinkDbContext).Assembly);

        // Aplicar convenciones de BaseEntity para todas las entidades que lo hereden
        ApplyBaseEntityConventions(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ApplyBaseEntityConventions(ModelBuilder modelBuilder)
    {
        foreach (
            var entity in modelBuilder
                .Model.GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
        )
        {
            // RowVersion para concurrencia optimista
            modelBuilder.Entity(entity.Name).Property<byte[]>("RowVersion").IsRowVersion();

            // CreatedAt con default value
            modelBuilder
                .Entity(entity.Name)
                .Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAdd();

            // UpdatedAt nullable
            modelBuilder.Entity(entity.Name).Property<DateTime?>("UpdatedAt");

            // DeleteAt nullable
            modelBuilder.Entity(entity.Name).Property<DateTime?>("DeleteAt");
        }
    }
}
