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

        // Properties
        builder.Property(p => p.RoomId).IsRequired();

        builder.Property(p => p.ParticipantType).IsRequired();

        builder.Property(p => p.TaxUserId);
        builder.Property(p => p.CustomerId);
        builder.Property(p => p.CompanyId);

        builder.Property(p => p.AddedByCompanyId).IsRequired();

        builder.Property(p => p.AddedByTaxUserId).IsRequired();

        builder.Property(p => p.Role).IsRequired();

        builder.Property(p => p.JoinedAt).IsRequired();

        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(p => p.IsMuted).IsRequired().HasDefaultValue(false);

        builder.Property(p => p.IsVideoEnabled).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.RoomId).HasDatabaseName("IX_RoomParticipants_RoomId");

        builder.HasIndex(p => p.TaxUserId).HasDatabaseName("IX_RoomParticipants_TaxUserId");

        builder.HasIndex(p => p.CustomerId).HasDatabaseName("IX_RoomParticipants_CustomerId");

        builder.HasIndex(p => p.CompanyId).HasDatabaseName("IX_RoomParticipants_CompanyId");

        builder
            .HasIndex(p => new { p.RoomId, p.TaxUserId })
            .HasDatabaseName("IX_RoomParticipants_Room_TaxUser")
            .IsUnique()
            .HasFilter("[TaxUserId] IS NOT NULL");

        builder
            .HasIndex(p => new { p.RoomId, p.CustomerId })
            .HasDatabaseName("IX_RoomParticipants_Room_Customer")
            .IsUnique()
            .HasFilter("[CustomerId] IS NOT NULL");

        // Constraints
        builder.ToTable(b =>
            b.HasCheckConstraint(
                "CK_RoomParticipant_ValidParticipant",
                "([ParticipantType] = 0 AND [TaxUserId] IS NOT NULL AND [CustomerId] IS NULL) OR "
                    + "([ParticipantType] = 1 AND [CustomerId] IS NOT NULL AND [TaxUserId] IS NULL)"
            )
        );

        // Relationships
        builder
            .HasOne(p => p.Room)
            .WithMany(r => r.Participants)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
