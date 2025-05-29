using AuthService.Domains.Companies;
using AuthService.Domains.Permissions;
using AuthService.Domains.Roles;
using AuthService.Domains.Sessions;
using AuthService.Domains.Users;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Context;

public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<TaxUser> TaxUsers { get; set; }
    public DbSet<TaxUserProfile> TaxUserProfiles { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermissions> RolePermissions { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Company> Companies { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TaxUser>().ToTable("TaxUsers");
        modelBuilder.Entity<TaxUserProfile>().ToTable("TaxUserProfiles");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<RolePermissions>().ToTable("RolePermissions");
        modelBuilder.Entity<Permission>().ToTable("Permissions");
        modelBuilder.Entity<Session>().ToTable("Sessions");
        modelBuilder.Entity<Company>().ToTable("Companies");

        modelBuilder.Entity<TaxUserProfile>().HasKey(t => t.Id);
        modelBuilder.Entity<TaxUser>().HasKey(t => t.Id);
        modelBuilder.Entity<Role>().HasKey(t => t.Id);
        modelBuilder.Entity<RolePermissions>().HasKey(t => t.Id);
        modelBuilder.Entity<Permission>().HasKey(t => t.Id);
        modelBuilder.Entity<Session>().HasKey(t => t.Id);
        modelBuilder.Entity<Company>().HasKey(t => t.Id);

        // Relación TaxUser - TaxUserProfile (1:1)
        modelBuilder.Entity<TaxUser>()
            .HasOne(u => u.TaxUserProfile)
            .WithOne(p => p.TaxUser)
            .HasForeignKey<TaxUserProfile>(p => p.TaxUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación TaxUser - Role (N:1)
        modelBuilder.Entity<TaxUser>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación TaxUser - Company (N:1)
        modelBuilder.Entity<TaxUser>()
            .HasOne(u => u.Company)
            .WithMany(c => c.TaxUsers)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relación TaxUser - Sessions (1:N) - FIXED
        modelBuilder.Entity<Session>()
            .HasOne(s => s.TaxUser)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.TaxUserId)
            .OnDelete(DeleteBehavior.Cascade);

        //todo Permission data default
        modelBuilder.Entity<Permission>().HasData(
            new Permission
            {
                Id = new Guid("550e8400-e29b-41d4-a716-446655440001"),
                Name = "Write",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = new Guid("550e8400-e29b-41d4-a716-446655440002"),
                Name = "Reader",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = new Guid("550e8400-e29b-41d4-a716-446655440003"),
                Name = "View",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = new Guid("550e8400-e29b-41d4-a716-446655440004"),
                Name = "Delete",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = new Guid("550e8400-e29b-41d4-a716-446655440005"),
                Name = "Update",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            });

        //todo Role data default
        modelBuilder.Entity<Role>().HasData(
        new Role
        {
            Id = new Guid("550e8400-e29b-41d4-a716-446655441001"),
            Name = "Administrator",
            Description = "Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform."
        }, new Role
        {
            Id = new Guid("550e8400-e29b-41d4-a716-446655441002"),
            Name = "User",
            Description = "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality"
        });
    }
}