using CommLinkService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommLinkService.Infrastructure.Persistence.Configurations;

public sealed class RoomParticipantConfiguration : IEntityTypeConfiguration<RoomParticipant>
{
    public void Configure(EntityTypeBuilder<RoomParticipant> builder)
    {
        builder.ToTable("RoomParticipants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.RoomId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Role).IsRequired();
        builder.Property(p => p.JoinedAt).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.IsMuted).IsRequired();
        builder.Property(p => p.IsVideoEnabled).IsRequired();

        builder.HasIndex(p => p.RoomId);
        builder.HasIndex(p => p.UserId);

        // ✅ Relación bien definida
        builder
            .HasOne(p => p.Room)
            .WithMany(r => r.Participants)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
