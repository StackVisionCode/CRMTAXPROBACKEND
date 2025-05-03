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
     

    }

// Introducing FOREIGN KEY constraint 'FK_RolePermissions_TaxUsers_TaxUserId' on table 'RolePermissions' may cause cycles or multiple cascade paths. Specify ON DELETE NO ACTION or ON UPDATE NO ACTION, or modify other FOREIGN KEY constraints.
// Could not create constraint or index. See previous errors.

}