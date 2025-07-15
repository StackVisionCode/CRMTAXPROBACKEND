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

        builder.Property(m => m.Content).IsRequired().HasMaxLength(4000);

        builder.Property(m => m.Type).IsRequired();

        builder.Property(m => m.Metadata).HasColumnType("nvarchar(max)");

        builder.Property(m => m.SentAt).IsRequired();
        builder.Property(m => m.IsDeleted).IsRequired();

        builder
            .HasMany(m => m.Reactions)
            .WithOne()
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.RoomId);
        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.SentAt);
    }
}
