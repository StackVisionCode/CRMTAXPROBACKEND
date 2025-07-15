using CommLinkService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommLinkService.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.Property(r => r.Type).IsRequired();
        builder.Property(r => r.CreatedBy).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.LastActivityAt);
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.MaxParticipants).HasDefaultValue(10);

        builder
            .HasMany(r => r.Participants)
            .WithOne()
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(r => r.Messages)
            .WithOne()
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.LastActivityAt);
    }
}
