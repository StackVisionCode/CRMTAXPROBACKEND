// Infrastructure/Context/CalendarDbContext.cs
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class CalendarDbContext : DbContext
{
    public CalendarDbContext(DbContextOptions<CalendarDbContext> options) : base(options) { }

    public DbSet<CalendarEvents> CalendarEvents => Set<CalendarEvents>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // TPH con discriminador
        b.Entity<CalendarEvents>()
         .ToTable("CalendarEvents")
         .HasDiscriminator<string>("Type")
         .HasValue<Appointment>("appointment")
         .HasValue<Meeting>("meeting");

        b.Entity<CalendarEvents>()
         .Property(e => e.RowVersion)
         .IsRowVersion();

        // Índices útiles
        b.Entity<CalendarEvents>()
         .HasIndex(e => new { e.UserId, e.StartUtc, e.EndUtc });

        // Participants (1:N)
        b.Entity<EventParticipant>()
         .HasOne(p => p.Meeting)
         .WithMany(m => m.Participants)
         .HasForeignKey(p => p.MeetingId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
