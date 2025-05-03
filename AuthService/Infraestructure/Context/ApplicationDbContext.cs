using AuthService.Domains.Permissions;
using AuthService.Domains.Roles;
using AuthService.Domains.Sessions;
using AuthService.Domains.Users;
using Microsoft.EntityFrameworkCore;
using Users;

namespace Infraestructure.Context;

public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<TaxUser> TaxUsers { get; set; }
    public DbSet<TaxUserType> TaxUserTypes { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermissions> RolePermissions { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Session> Sessions { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TaxUser>().ToTable("TaxUsers");
        modelBuilder.Entity<TaxUserType>().ToTable("TaxUserTypes");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<RolePermissions>().ToTable("RolePermissions");
        modelBuilder.Entity<Permission>().ToTable("Permissions");
        modelBuilder.Entity<Session>().ToTable("Sessions");

        modelBuilder.Entity<TaxUserType>().HasKey(t => t.Id);
        modelBuilder.Entity<TaxUser>().HasKey(t => t.Id);
        modelBuilder.Entity<Role>().HasKey(t => t.Id);
        modelBuilder.Entity<RolePermissions>().HasKey(t => t.Id);
        modelBuilder.Entity<Permission>().HasKey(t => t.Id);
        modelBuilder.Entity<Session>().HasKey(t => t.Id);


        modelBuilder.Entity<RolePermissions>()
            .HasOne(rp => rp.TaxUser)
            .WithMany(tu => tu.RolePermissions)
            .HasForeignKey(rp => rp.TaxUserId)
            .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues
                                                //todo permissions data default
        modelBuilder.Entity<Permission>().HasData(
            new Permission
            {
                Id = 1,
                Name = "Write",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = 2,
                Name = "Reader",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = 3,
                Name = "View",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = 4,
                Name = "Delete",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            }, new Permission
            {
                Id = 5,
                Name = "Update",
                RolePermissions = new List<RolePermissions>() // Initialize as an empty list or provide appropriate values
            });

        //todo TaxUserType data default
        modelBuilder.Entity<TaxUserType>().HasData(
          new TaxUserType { 
              Id = 1, 
              Name = "Owner", 
              Description = "SuperUsuario" 
              },
          new TaxUserType { 
            Id = 2, 
            Name = "Client", 
            Description = "Cliente" 
            },
          new TaxUserType { 
            Id = 3, 
            Name = "Staff", 
            Description = "Empleado" 
            }
      );

        //todo Role data default
        modelBuilder.Entity<Role>().HasData(
        new Role
        {
          Id = 1,
          Name = "Administrator",
          Description = "Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform.",
          TaxUser = null // Set this to a valid TaxUser instance if required

        }, new Role
        {
          Id = 2,
          Name = "User",
          Description = "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality",
          TaxUser = null // Set this to a valid TaxUser instance if required
        });

        base.OnModelCreating(modelBuilder);
    }
}