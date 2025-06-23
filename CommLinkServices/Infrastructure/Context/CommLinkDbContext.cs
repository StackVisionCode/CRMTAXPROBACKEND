using CommLinkServices.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommLinkServices.Infrastructure.Context;

public class CommLinkDbContext : DbContext
{
    public CommLinkDbContext(DbContextOptions<CommLinkDbContext> options)
        : base(options) { }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Call> Calls => Set<Call>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Conversation>()
            .HasIndex(c => new { c.FirstUserId, c.SecondUserId })
            .IsUnique();

        modelBuilder.Entity<Message>().HasIndex(m => new { m.ConversationId, m.SentAt });
    }
}
