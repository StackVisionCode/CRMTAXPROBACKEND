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

        builder.Property(r => r.MessageId).IsRequired();
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.Emoji).IsRequired().HasMaxLength(50);
        builder.Property(r => r.ReactedAt).IsRequired();

        builder.HasIndex(r => r.MessageId);
        builder.HasIndex(r => r.UserId);
    }
}
