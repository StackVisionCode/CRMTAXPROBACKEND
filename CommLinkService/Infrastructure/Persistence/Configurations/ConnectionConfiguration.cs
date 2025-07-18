using CommLinkService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommLinkService.Infrastructure.Persistence.Configurations;

public sealed class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.ToTable("Connections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.ConnectionId).IsRequired().HasMaxLength(256);
        builder.Property(c => c.ConnectedAt).IsRequired();
        builder.Property(c => c.DisconnectedAt);
        builder.Property(c => c.UserAgent).HasMaxLength(512);
        builder.Property(c => c.IpAddress).HasMaxLength(64);
        builder.Property(c => c.IsActive).IsRequired();

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.IsActive);
    }
}
