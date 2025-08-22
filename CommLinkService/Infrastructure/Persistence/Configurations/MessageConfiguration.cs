using CommLinkService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommLinkService.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(m => m.Id);

        // Properties
        builder.Property(m => m.RoomId).IsRequired();

        builder.Property(m => m.SenderType).IsRequired();

        builder.Property(m => m.SenderTaxUserId);
        builder.Property(m => m.SenderCustomerId);
        builder.Property(m => m.SenderCompanyId);

        builder.Property(m => m.Content).IsRequired().HasMaxLength(4000);

        builder.Property(m => m.Type).IsRequired();

        builder.Property(m => m.SentAt).IsRequired();

        builder.Property(m => m.EditedAt);

        builder.Property(m => m.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.Property(m => m.Metadata).HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(m => m.RoomId).HasDatabaseName("IX_Messages_RoomId");

        builder.HasIndex(m => m.SenderTaxUserId).HasDatabaseName("IX_Messages_SenderTaxUserId");

        builder.HasIndex(m => m.SenderCustomerId).HasDatabaseName("IX_Messages_SenderCustomerId");

        builder.HasIndex(m => m.SentAt).HasDatabaseName("IX_Messages_SentAt");

        builder
            .HasIndex(m => new { m.RoomId, m.SentAt })
            .HasDatabaseName("IX_Messages_Room_SentAt");

        // Constraints
        builder.ToTable(b =>
            b.HasCheckConstraint(
                "CK_Message_ValidSender",
                "([SenderType] = 0 AND [SenderTaxUserId] IS NOT NULL AND [SenderCustomerId] IS NULL) OR "
                    + "([SenderType] = 1 AND [SenderCustomerId] IS NOT NULL AND [SenderTaxUserId] IS NULL)"
            )
        );

        // Relationships
        builder
            .HasOne(m => m.Room)
            .WithMany(r => r.Messages)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(m => m.Reactions)
            .WithOne(r => r.Message)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
