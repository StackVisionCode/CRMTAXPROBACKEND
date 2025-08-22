using CommLinkService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommLinkService.Infrastructure.Persistence.Configurations;

public sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.MessageId).IsRequired();

        builder.Property(r => r.ReactorType).IsRequired();

        builder.Property(r => r.ReactorTaxUserId);
        builder.Property(r => r.ReactorCustomerId);
        builder.Property(r => r.ReactorCompanyId);

        builder.Property(r => r.Emoji).IsRequired().HasMaxLength(50);

        builder.Property(r => r.ReactedAt).IsRequired();

        // Indexes
        builder.HasIndex(r => r.MessageId).HasDatabaseName("IX_MessageReactions_MessageId");

        builder
            .HasIndex(r => r.ReactorTaxUserId)
            .HasDatabaseName("IX_MessageReactions_ReactorTaxUserId");

        builder
            .HasIndex(r => r.ReactorCustomerId)
            .HasDatabaseName("IX_MessageReactions_ReactorCustomerId");

        // Unique constraint para evitar reacciones duplicadas
        builder
            .HasIndex(r => new
            {
                r.MessageId,
                r.ReactorTaxUserId,
                r.Emoji,
            })
            .HasDatabaseName("IX_MessageReactions_Message_TaxUser_Emoji")
            .IsUnique()
            .HasFilter("[ReactorTaxUserId] IS NOT NULL");

        builder
            .HasIndex(r => new
            {
                r.MessageId,
                r.ReactorCustomerId,
                r.Emoji,
            })
            .HasDatabaseName("IX_MessageReactions_Message_Customer_Emoji")
            .IsUnique()
            .HasFilter("[ReactorCustomerId] IS NOT NULL");

        // Constraints
        builder.ToTable(b =>
            b.HasCheckConstraint(
                "CK_MessageReaction_ValidReactor",
                "([ReactorType] = 0 AND [ReactorTaxUserId] IS NOT NULL AND [ReactorCustomerId] IS NULL) OR "
                    + "([ReactorType] = 1 AND [ReactorCustomerId] IS NOT NULL AND [ReactorTaxUserId] IS NULL)"
            )
        );

        // Relationships
        builder
            .HasOne(r => r.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
