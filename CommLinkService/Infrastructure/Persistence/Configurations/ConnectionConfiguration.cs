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

        // Properties
        builder.Property(c => c.UserType).IsRequired();

        builder.Property(c => c.TaxUserId);
        builder.Property(c => c.CustomerId);
        builder.Property(c => c.CompanyId);

        builder.Property(c => c.ConnectionId).IsRequired().HasMaxLength(256);

        builder.Property(c => c.ConnectedAt).IsRequired();

        builder.Property(c => c.DisconnectedAt);

        builder.Property(c => c.UserAgent).HasMaxLength(512);

        builder.Property(c => c.IpAddress).HasMaxLength(64);

        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);

        // Indexes
        builder.HasIndex(c => c.TaxUserId).HasDatabaseName("IX_Connections_TaxUserId");

        builder.HasIndex(c => c.CustomerId).HasDatabaseName("IX_Connections_CustomerId");

        builder
            .HasIndex(c => c.ConnectionId)
            .HasDatabaseName("IX_Connections_ConnectionId")
            .IsUnique();

        builder.HasIndex(c => c.IsActive).HasDatabaseName("IX_Connections_IsActive");

        // Constraints
        builder.ToTable(b =>
            b.HasCheckConstraint(
                "CK_Connection_ValidUser",
                "([UserType] = 0 AND [TaxUserId] IS NOT NULL AND [CustomerId] IS NULL) OR "
                    + "([UserType] = 1 AND [CustomerId] IS NOT NULL AND [TaxUserId] IS NULL)"
            )
        );
    }
}
