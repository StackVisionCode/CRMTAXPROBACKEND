using AuthService.Domains.Companies;
using AuthService.Domains.Permissions;
using AuthService.Domains.Roles;
using AuthService.Domains.Sessions;
using AuthService.Domains.Users;
using Common;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<TaxUser> TaxUsers { get; set; }
    public DbSet<TaxUserProfile> TaxUserProfiles { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<CustomerRole> CustomerRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<CustomerSession> CustomerSessions { get; set; }
    public DbSet<Company> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica la convención de RowVersion para **todas** las entidades que hereden de BaseEntity (Seguimiento de versiones)
        foreach (
            var entity in modelBuilder
                .Model.GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
        )
        {
            modelBuilder.Entity(entity.Name).Property<byte[]>("RowVersion").IsRowVersion();
        }

        // Aplica la convención de CreatedAt y UpdatedAt para **todas** las entidades que hereden de BaseEntity
        foreach (
            var entity in modelBuilder
                .Model.GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
        )
        {
            modelBuilder
                .Entity(entity.Name)
                .Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("GETUTCDATE()") // valor lo pone SQL Server
                .ValueGeneratedOnAdd();

            // UpdatedAt   (nullable, sin default; solo la declaramos)
            modelBuilder.Entity(entity.Name).Property<DateTime?>("UpdatedAt");

            // DeleteAt/DeletedAt (opcional, por coherencia)
            modelBuilder.Entity(entity.Name).Property<DateTime?>("DeleteAt");
        }

        modelBuilder.Entity<TaxUser>().ToTable("TaxUsers");
        modelBuilder.Entity<TaxUserProfile>().ToTable("TaxUserProfiles");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<RolePermission>().ToTable("RolePermissions");
        modelBuilder.Entity<UserRole>().ToTable("UserRoles");
        modelBuilder.Entity<CustomerRole>().ToTable("CustomerRoles");
        modelBuilder.Entity<Permission>().ToTable("Permissions");
        modelBuilder.Entity<Session>().ToTable("Sessions");
        modelBuilder.Entity<CustomerSession>().ToTable("CustomerSessions");
        modelBuilder.Entity<Company>().ToTable("Companies");

        modelBuilder.Entity<TaxUserProfile>().HasKey(t => t.Id);
        modelBuilder.Entity<TaxUser>().HasKey(t => t.Id);
        modelBuilder.Entity<Role>().HasKey(t => t.Id);
        modelBuilder.Entity<RolePermission>().HasKey(t => t.Id);
        modelBuilder.Entity<UserRole>().HasKey(t => t.Id);
        modelBuilder.Entity<CustomerRole>().HasKey(t => t.Id);
        modelBuilder.Entity<Permission>().HasKey(t => t.Id);
        modelBuilder.Entity<Session>().HasKey(t => t.Id);
        modelBuilder.Entity<CustomerSession>().HasKey(t => t.Id);
        modelBuilder.Entity<Company>().HasKey(t => t.Id);

        // Role.Name (no duplicados)
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();

        // Permission.Code (clave funcional)
        modelBuilder.Entity<Permission>().HasIndex(p => p.Code).IsUnique();

        // RolePermission (Relación Role <-> Permission)
        modelBuilder
            .Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique();

        // Relaciones explícitas
        modelBuilder
            .Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserRole (Relación TaxUser <-> Role)
        modelBuilder.Entity<UserRole>().HasIndex(ur => new { ur.TaxUserId, ur.RoleId }).IsUnique();

        modelBuilder
            .Entity<UserRole>()
            .HasOne(ur => ur.TaxUser)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.TaxUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // CustomerRole (Relación Customer <-> Role)
        modelBuilder
            .Entity<CustomerRole>()
            .HasIndex(cr => new { cr.CustomerId, cr.RoleId })
            .IsUnique();

        modelBuilder
            .Entity<CustomerRole>()
            .HasOne(cr => cr.Role)
            .WithMany() // No navegación inversa necesaria
            .HasForeignKey(cr => cr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaxUserProfile (1 : 1)
        modelBuilder
            .Entity<TaxUser>()
            .HasOne(u => u.TaxUserProfile)
            .WithOne(p => p.TaxUser)
            .HasForeignKey<TaxUserProfile>(p => p.TaxUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaxUser – Company (N : 1)
        modelBuilder
            .Entity<TaxUser>()
            .HasOne(u => u.Company)
            .WithMany(c => c.TaxUsers)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relación TaxUser - Sessions (1:N) - FIXED
        modelBuilder
            .Entity<Session>()
            .HasOne(s => s.TaxUser)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.TaxUserId)
            .OnDelete(DeleteBehavior.Cascade);

        //todo Permission data default
        modelBuilder
            .Entity<Permission>()
            .HasData(
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440001"),
                    Name = "Create Permissions",
                    Code = "Permission.Create",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440002"),
                    Name = "Read Permissions",
                    Code = "Permission.Read",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440003"),
                    Name = "View Permissions",
                    Code = "Permission.View",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440004"),
                    Name = "Delete Permissions",
                    Code = "Permission.Delete",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440005"),
                    Name = "Update Permissions",
                    Code = "Permission.Update",
                    RolePermissions = new List<RolePermission>(),
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440006"),
                    Name = "Create TaxUsers",
                    Code = "TaxUser.Create",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440007"),
                    Name = "Read TaxUsers",
                    Code = "TaxUser.Read",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440008"),
                    Name = "View TaxUsers",
                    Code = "TaxUser.View",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440009"),
                    Name = "Delete TaxUsers",
                    Code = "TaxUser.Delete",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440010"),
                    Name = "Update TaxUsers",
                    Code = "TaxUser.Update",
                    RolePermissions = new List<RolePermission>(),
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440011"),
                    Name = "Create Customers",
                    Code = "Customer.Create",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440012"),
                    Name = "Read Customers",
                    Code = "Customer.Read",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440013"),
                    Name = "View Customers",
                    Code = "Customer.View",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440014"),
                    Name = "Delete Customers",
                    Code = "Customer.Delete",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440015"),
                    Name = "Update Customers",
                    Code = "Customer.Update",
                    RolePermissions = new List<RolePermission>(),
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440016"),
                    Name = "Create Roles",
                    Code = "Role.Create",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440017"),
                    Name = "Read Roles",
                    Code = "Role.Read",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440018"),
                    Name = "View Roles",
                    Code = "Role.View",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440019"),
                    Name = "Delete Roles",
                    Code = "Role.Delete",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440020"),
                    Name = "Update Roles",
                    Code = "Role.Update",
                    RolePermissions = new List<RolePermission>(),
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440021"),
                    Name = "Create RolePermissions",
                    Code = "RolePermission.Create",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440022"),
                    Name = "Read RolePermissions",
                    Code = "RolePermission.Read",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440023"),
                    Name = "View RolePermissions",
                    Code = "RolePermission.View",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440024"),
                    Name = "Delete RolePermissions",
                    Code = "RolePermission.Delete",
                    RolePermissions = new List<RolePermission>(), // Initialize as an empty list or provide appropriate values
                },
                new Permission
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655440025"),
                    Name = "Update RolePermissions",
                    Code = "RolePermission.Update",
                    RolePermissions = new List<RolePermission>(),
                },
                new Permission
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440026"),
                    Name = "Read own profile",
                    Code = "Customer.SelfRead",
                }
            );

        // ------------------------------------------------------------------
        // 0.  PERMISOS NUEVOS
        // ------------------------------------------------------------------
        var extraPerms = new[]
        {
            (27, "Disable customer login", "Customer.DisableLogin"),
            (28, "Enable customer login", "Customer.EnableLogin"),
            (29, "Read sessions", "Sessions.Read"),
            (30, "Create dependent", "Dependent.Create"),
            (31, "Update dependent", "Dependent.Update"),
            (32, "Delete dependent", "Dependent.Delete"),
            (33, "Read dependent", "Dependent.Read"),
            (34, "View dependent", "Dependent.Viewer"),
            (35, "Create tax info", "TaxInformation.Create"),
            (36, "Update tax info", "TaxInformation.Update"),
            (37, "Delete tax info", "TaxInformation.Delete"),
            (38, "Read tax info", "TaxInformation.Read"),
            (39, "View tax info", "TaxInformation.Viewer"),
        }.Select(t => new Permission
        {
            Id = NewPerm(t.Item1),
            Name = t.Item2,
            Code = t.Item3,
        });

        modelBuilder.Entity<Permission>().HasData(extraPerms);

        // ------------------------------------------------------------------
        // 1. Seeding de datos iniciales
        // ------------------------------------------------------------------
        SeedCustomerRolePermissions(modelBuilder);
        // ------------------------------------------------------------------
        // 2.  ROLE-PERMISSIONS  Administrator  (ahora 1-39)
        // ------------------------------------------------------------------
        SeedAdministratorRolePermissions(modelBuilder); // ← ver método re-hecho abajo
        // ------------------------------------------------------------------
        // 3.  ROLE-PERMISSIONS  TaxPreparer
        // ------------------------------------------------------------------
        SeedTaxPreparerRolePermissions(modelBuilder);
        // ------------------------------------------------------------------
        // 4.  COMPANY + ADMIN USER
        // ------------------------------------------------------------------
        SeedCompanyAndAdmin(modelBuilder);

        //todo Role data default
        modelBuilder
            .Entity<Role>()
            .HasData(
                new Role
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655441001"),
                    Name = "Administrator",
                    Description =
                        "Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform.",
                    PortalAccess = PortalAccess.Staff,
                },
                new Role
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655441002"),
                    Name = "User",
                    Description =
                        "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality",
                    PortalAccess = PortalAccess.Staff,
                },
                new Role
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655441003"),
                    Name = "TaxPreparer",
                    Description =
                        "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality",
                    PortalAccess = PortalAccess.Staff,
                },
                new Role
                {
                    Id = new Guid("550e8400-e29b-41d4-a716-446655441004"),
                    Name = "Customer",
                    Description =
                        "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality",
                    PortalAccess = PortalAccess.Customer,
                }
            );
    }

    private static void SeedCustomerRolePermissions(ModelBuilder modelBuilder)
    {
        var customerRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655441004");
        var selfReadPermId = Guid.Parse("550e8400-e29b-41d4-a716-446655440026");
        var selfReadRpId = Guid.Parse("770e8400-e29b-41d4-a716-556655450026");

        modelBuilder
            .Entity<RolePermission>()
            .HasData(
                new RolePermission
                {
                    Id = selfReadRpId,
                    RoleId = customerRoleId,
                    PermissionId = selfReadPermId,
                }
            );
    }

    // private static void SeedAdministratorRolePermissions(ModelBuilder modelBuilder)
    // {
    //     var adminId = Guid.Parse("550e8400-e29b-41d4-a716-446655441001");

    //     // IDs 001-025 → asignar a Administrator
    //     var entries = Enumerable
    //         .Range(1, 25)
    //         .Select(i =>
    //         {
    //             var permGuid = Guid.Parse(
    //                 $"550e8400-e29b-41d4-a716-44665544{(i).ToString("0000")}"
    //             );
    //             var rolePermGuid = Guid.Parse(
    //                 $"660e8400-e29b-41d4-a716-44665545{(i).ToString("0000")}"
    //             );
    //             return new RolePermission
    //             {
    //                 Id = rolePermGuid,
    //                 RoleId = adminId,
    //                 PermissionId = permGuid,
    //             };
    //         });

    //     modelBuilder.Entity<RolePermission>().HasData(entries);
    // }

    private static Guid NewPerm(int n) => Guid.Parse($"550e8400-e29b-41d4-a716-44665544{n:0000}");

    static readonly Guid CompanySeedId = Guid.Parse("770e8400-e29b-41d4-a716-556655441000");
    static readonly Guid AdminUserSeedId = Guid.Parse("880e8400-e29b-41d4-a716-556655441000");
    static readonly Guid AdministratorRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655441001");
    static readonly Guid TaxPreparerRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655441003");

    private static void SeedAdministratorRolePermissions(ModelBuilder mb)
    {
        // ahora cubrimos 1..39
        var entries = Enumerable
            .Range(1, 39)
            .Select(i => new RolePermission
            {
                Id = Guid.Parse($"660e8400-e29b-41d4-a716-44665545{i:0000}"),
                RoleId = AdministratorRoleId,
                PermissionId = Guid.Parse($"550e8400-e29b-41d4-a716-44665544{i:0000}"),
            });

        mb.Entity<RolePermission>().HasData(entries);
    }

    private static void SeedTaxPreparerRolePermissions(ModelBuilder mb)
    {
        // lista blanca según tu especificación
        var allowedCodes = new[]
        {
            // Customer
            "Customer.Create",
            "Customer.Read",
            "Customer.View",
            "Customer.Update",
            "Customer.DisableLogin",
            "Customer.EnableLogin",
            // Permission
            "Permission.Create",
            "Permission.Delete",
            // Dependent
            "Dependent.Create",
            "Dependent.Update",
            "Dependent.Delete",
            "Dependent.Read",
            "Dependent.Viewer",
            // TaxInformation
            "TaxInformation.Create",
            "TaxInformation.Update",
            "TaxInformation.Delete",
            "TaxInformation.Read",
            "TaxInformation.Viewer",
        };

        var rp = allowedCodes.Select(
            (code, idx) =>
                new RolePermission
                {
                    Id = Guid.Parse($"770e8400-e29b-41d4-a716-55665546{idx:0000}"), // serie distinta
                    RoleId = TaxPreparerRoleId,
                    PermissionId = (Guid)
                        mb
                            .Model.FindEntityType(typeof(Permission))!
                            .GetSeedData()
                            .Cast<Dictionary<string, object>>()
                            .First(p => (string)p["Code"] == code)["Id"],
                }
        );
        mb.Entity<RolePermission>().HasData(rp);
    }

    private static void SeedCompanyAndAdmin(ModelBuilder mb)
    {
        /* --- company --- */
        mb.Entity<Company>()
            .HasData(
                new Company
                {
                    Id = CompanySeedId,
                    FullName = "Vision Software",
                    CompanyName = "StackVsion Sofwatre S.R.L.",
                    Phone = "8298981594",
                    Address = "Calle C, Brisa Oriental VIII",
                    Description = "Sofwatre Developers Assembly.",
                    UserLimit = 25,
                    Brand = "https://images5.example.com/",
                }
            );

        mb.Entity<TaxUser>()
            .HasData(
                new
                {
                    Id = AdminUserSeedId,
                    CompanyId = CompanySeedId,
                    Email = "stackvisionsoftware@gmail.com",
                    Password = "zBLVJHyDUQKSp3ZYdgIeOEDnoeD61Zg566QoP2165AQAPHxzvJlAWjt1dV+Qinc7",
                    Domain = "stackvision",
                    IsActive = true,
                    Confirm = true,
                    OtpVerified = false,
                }
            );
        /* --- user → Administrator role --- */
        mb.Entity<UserRole>()
            .HasData(
                new UserRole
                {
                    Id = Guid.Parse("880e8400-e29b-41d4-a716-556655442000"),
                    TaxUserId = AdminUserSeedId,
                    RoleId = AdministratorRoleId,
                }
            );
    }
}
