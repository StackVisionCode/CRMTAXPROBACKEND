using System.Text.Json;
using Common;
using Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Service> Services { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<CustomPlan> CustomPlans { get; set; }
    public DbSet<CustomModule> CustomModules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (
            var entity in modelBuilder
                .Model.GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
        )
        {
            modelBuilder.Entity(entity.Name).Property<byte[]>("RowVersion").IsRowVersion();
            modelBuilder
                .Entity(entity.Name)
                .Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity(entity.Name).Property<DateTime?>("UpdatedAt");
            modelBuilder.Entity(entity.Name).Property<DateTime?>("DeleteAt");
        }

        // Tables
        modelBuilder.Entity<Service>().ToTable("Services");
        modelBuilder.Entity<Module>().ToTable("Modules");
        modelBuilder.Entity<CustomPlan>().ToTable("CustomPlans");
        modelBuilder.Entity<CustomModule>().ToTable("CustomModules");

        modelBuilder
            .Entity<Service>()
            .Property(s => s.Features)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
                    ?? new List<string>()
            )
            .Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );

        // Configurar precisión para Service.Price
        modelBuilder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2); // 18 dígitos total, 2 decimales

        // Configurar precisión para CustomPlan.Price
        modelBuilder.Entity<CustomPlan>().Property(cp => cp.Price).HasPrecision(18, 2); // 18 dígitos total, 2 decimales

        // Service -> Module (1:N)
        modelBuilder
            .Entity<Module>()
            .HasOne(m => m.Service)
            .WithMany(s => s.Modules)
            .HasForeignKey(m => m.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // CustomModule (CustomPlan ↔ Module muchos a muchos)
        modelBuilder
            .Entity<CustomModule>()
            .HasIndex(cm => new { cm.CustomPlanId, cm.ModuleId })
            .IsUnique();

        modelBuilder
            .Entity<CustomModule>()
            .HasOne(cm => cm.CustomPlan)
            .WithMany(cp => cp.CustomModules)
            .HasForeignKey(cm => cm.CustomPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<CustomModule>()
            .HasOne(cm => cm.Module)
            .WithMany(m => m.CustomModules)
            .HasForeignKey(cm => cm.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        modelBuilder.Entity<Service>().HasIndex(s => s.Name).IsUnique();
        modelBuilder.Entity<Module>().HasIndex(m => m.Name).IsUnique();
        modelBuilder.Entity<CustomPlan>().HasIndex(cp => cp.CompanyId).IsUnique();
        modelBuilder.Entity<CustomPlan>().HasIndex(cp => cp.IsActive);
        modelBuilder.Entity<Module>().HasIndex(m => m.ServiceId);
        modelBuilder.Entity<CustomModule>().HasIndex(cm => cm.CustomPlanId);
        modelBuilder.Entity<CustomModule>().HasIndex(cm => cm.ModuleId);

        // Checks
        modelBuilder
            .Entity<Service>()
            .ToTable(b => b.HasCheckConstraint("CK_Services_UserLimit", "[UserLimit] >= 0"));

        modelBuilder
            .Entity<Service>()
            .ToTable(b => b.HasCheckConstraint("CK_Services_Price", "[Price] >= 0"));

        modelBuilder
            .Entity<CustomPlan>()
            .ToTable(b => b.HasCheckConstraint("CK_CustomPlans_Price", "[Price] >= 0"));

        modelBuilder
            .Entity<CustomPlan>()
            .ToTable(b => b.HasCheckConstraint("CK_CustomPlans_UserLimit", "[UserLimit] >= 1"));

        // Solo un constraint para fechas, usando RenewDate
        modelBuilder
            .Entity<CustomPlan>()
            .ToTable(b =>
                b.HasCheckConstraint("CK_CustomPlans_RenewDate", "[RenewDate] IS NOT NULL")
            );

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var basicServiceId = Guid.Parse("660e8400-e29b-41d4-a716-556655441001");
        var standardServiceId = Guid.Parse("660e8400-e29b-41d4-a716-556655441002");
        var proServiceId = Guid.Parse("660e8400-e29b-41d4-a716-556655441003");
        var DeveloperServiceId = Guid.Parse("660e8400-e29b-41d4-a716-556655441004");

        // Services
        modelBuilder
            .Entity<Service>()
            .HasData(
                new Service
                {
                    Id = basicServiceId,
                    Name = "Basic",
                    Title = "Basic Plan",
                    Description = "Basic tax preparation service with essential features",
                    Features = new List<string>
                    {
                        "Individual tax returns",
                        "Basic invoicing",
                        "Document storage",
                        "Email support",
                    },
                    Price = 29.99m,
                    UserLimit = 1,
                    ServiceLevel = ServiceLevel.Basic,
                    IsActive = true,
                },
                new Service
                {
                    Id = standardServiceId,
                    Name = "Standard",
                    Title = "Standard Plan",
                    Description = "Standard service with additional modules and more users",
                    Features = new List<string>
                    {
                        "Individual & business tax returns",
                        "Advanced invoicing",
                        "Document management",
                        "Financial reports",
                        "Customer portal",
                        "Priority support",
                    },
                    Price = 59.99m,
                    UserLimit = 4,
                    ServiceLevel = ServiceLevel.Standard,
                    IsActive = true,
                },
                new Service
                {
                    Id = proServiceId,
                    Name = "Pro",
                    Title = "Professional Plan",
                    Description = "Professional service with all modules and unlimited features",
                    Features = new List<string>
                    {
                        "All tax return types",
                        "Complete invoicing suite",
                        "Advanced document management",
                        "Comprehensive reports",
                        "Full customer portal",
                        "Advanced analytics",
                        "API integrations",
                        "White label options",
                        "24/7 premium support",
                    },
                    Price = 99.99m,
                    UserLimit = 5,
                    ServiceLevel = ServiceLevel.Pro,
                    IsActive = true,
                },
                new Service
                {
                    Id = DeveloperServiceId,
                    Name = "Developer",
                    Title = "Developer Access",
                    Description = "Unlimited access for system developers and administrators",
                    Features = new List<string>
                    {
                        "Full System Access",
                        "Unlimited Users",
                        "All Modules",
                        "Developer Tools",
                        "System Administration",
                    },
                    Price = 0m,
                    UserLimit = int.MaxValue,
                    IsActive = true,
                }
            );

        // Modules
        var moduleData = new[]
        {
            (
                1,
                "Tax Returns",
                "Individual and business tax return preparation",
                "/tax-returns",
                basicServiceId
            ),
            (2, "Invoicing", "Create and manage invoices", "/invoicing", basicServiceId),
            (
                3,
                "Document Management",
                "Upload and organize tax documents",
                "/documents",
                basicServiceId
            ),
            (4, "Reports", "Generate financial and tax reports", "/reports", standardServiceId),
            (
                5,
                "Customer Portal",
                "Dedicated portal for client communication",
                "/customer-portal",
                standardServiceId
            ),
            (
                6,
                "Advanced Analytics",
                "Business insights and analytics",
                "/analytics",
                proServiceId
            ),
            (
                7,
                "API Integration",
                "Connect with third-party services",
                "/api-integration",
                proServiceId
            ),
            (8, "White Label", "Custom branding options", "/white-label", proServiceId),
            (
                9,
                "Multi-Company Management",
                "Manage multiple companies from one dashboard",
                "/multi-company",
                (Guid?)null
            ),
            (
                10,
                "Advanced Security",
                "Enhanced security features and compliance",
                "/security",
                (Guid?)null
            ),
        }.Select(m => new Module
        {
            Id = Guid.Parse($"770e8400-e29b-41d4-a716-55665544{m.Item1:0000}"),
            Name = m.Item2,
            Description = m.Item3,
            Url = m.Item4,
            ServiceId = m.Item5,
            IsActive = true,
        });

        modelBuilder.Entity<Module>().HasData(moduleData);
    }
}
