using CommLinkService.Domain.Entities;
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommLinkDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
