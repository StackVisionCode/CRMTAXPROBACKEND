using CommLinkService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommLinkService.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.Property(r => r.Type).IsRequired();

        builder.Property(r => r.CreatedByCompanyId).IsRequired();

        builder.Property(r => r.CreatedByTaxUserId).IsRequired();

        builder.Property(r => r.LastModifiedByTaxUserId);

        builder.Property(r => r.LastActivityAt);

        builder.Property(r => r.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(r => r.MaxParticipants).HasDefaultValue(10);

        // Indexes
        builder.HasIndex(r => r.CreatedByCompanyId).HasDatabaseName("IX_Rooms_CreatedByCompanyId");

        builder.HasIndex(r => r.CreatedByTaxUserId).HasDatabaseName("IX_Rooms_CreatedByTaxUserId");

        builder.HasIndex(r => r.LastActivityAt).HasDatabaseName("IX_Rooms_LastActivityAt");

        builder.HasIndex(r => new { r.Type, r.IsActive }).HasDatabaseName("IX_Rooms_Type_IsActive");

        // Relationships
        builder
            .HasMany(r => r.Participants)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(r => r.Messages)
            .WithOne(m => m.Room)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
