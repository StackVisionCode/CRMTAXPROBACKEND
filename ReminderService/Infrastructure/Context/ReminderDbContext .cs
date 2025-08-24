using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class ReminderDbContext : DbContext
{
    public ReminderDbContext(DbContextOptions<ReminderDbContext> options) : base(options) { }

    public DbSet<Reminder> Reminders => Set<Reminder>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Reminder>().ToTable("Reminders");
        b.Entity<Reminder>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<Reminder>().HasIndex(r => new { r.AggregateType, r.AggregateId });
        b.Entity<Reminder>().HasIndex(r => r.RemindAtUtc);
    }
}
