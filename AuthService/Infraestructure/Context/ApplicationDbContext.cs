using AuthService.Domains.Addresses;
using AuthService.Domains.Companies;
using AuthService.Domains.Geography;
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
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<CustomerRole> CustomerRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<CustomerSession> CustomerSessions { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<State> States { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica la convención de RowVersion para **todas** las entidades que hereden de BaseEntity
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
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity(entity.Name).Property<DateTime?>("UpdatedAt");
            modelBuilder.Entity(entity.Name).Property<DateTime?>("DeleteAt");
        }

        // Aplicar las mismas configuraciones a entidades específicas que NO heredan de BaseEntity
        var additionalEntityTypes = new[] { typeof(State), typeof(Country) };

        foreach (var entityType in additionalEntityTypes)
        {
            var entityTypeInfo = modelBuilder.Model.FindEntityType(entityType);
            if (entityTypeInfo != null)
            {
                // RowVersion
                modelBuilder.Entity(entityType).Property<byte[]>("RowVersion").IsRowVersion();

                // CreatedAt con valor por defecto
                modelBuilder
                    .Entity(entityType)
                    .Property<DateTime>("CreatedAt")
                    .HasDefaultValueSql("GETUTCDATE()")
                    .ValueGeneratedOnAdd();

                // UpdatedAt y DeleteAt opcionales
                modelBuilder.Entity(entityType).Property<DateTime?>("UpdatedAt");
                modelBuilder.Entity(entityType).Property<DateTime?>("DeleteAt");
            }
        }

        // Configuración de tablas
        modelBuilder.Entity<TaxUser>().ToTable("TaxUsers");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<RolePermission>().ToTable("RolePermissions");
        modelBuilder.Entity<UserRole>().ToTable("UserRoles");
        modelBuilder.Entity<CustomerRole>().ToTable("CustomerRoles");
        modelBuilder.Entity<Permission>().ToTable("Permissions");
        modelBuilder.Entity<Session>().ToTable("Sessions");
        modelBuilder.Entity<CustomerSession>().ToTable("CustomerSessions");
        modelBuilder.Entity<Company>().ToTable("Companies");
        modelBuilder.Entity<Address>().ToTable("Addresses");
        modelBuilder.Entity<Country>().ToTable("Countries");
        modelBuilder.Entity<State>().ToTable("States");

        // Índices únicos
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<TaxUser>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Company>().HasIndex(c => c.Domain).IsUnique();

        // Checks
        modelBuilder
            .Entity<Company>()
            .ToTable(b => b.HasCheckConstraint("CK_Companies_UserLimit", "[UserLimit] >= 0"));

        // RolePermission (Relación Role <-> Permission)
        modelBuilder
            .Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique();

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
            .WithMany()
            .HasForeignKey(cr => cr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaxUser – Company (N : 1) - CAMBIO: Restrict en lugar de Cascade
        modelBuilder
            .Entity<TaxUser>()
            .HasOne(u => u.Company)
            .WithMany(c => c.TaxUsers)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación TaxUser - Sessions (1:N)
        modelBuilder
            .Entity<Session>()
            .HasOne(s => s.TaxUser)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.TaxUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // >>> Relaciones Address <<<

        // Catálogos: State -> Country (N:1)
        modelBuilder
            .Entity<State>()
            .HasOne(s => s.Country)
            .WithMany(c => c.States)
            .HasForeignKey(s => s.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Address -> Country/State (N:1)
        modelBuilder
            .Entity<Address>()
            .HasOne(a => a.Country)
            .WithMany()
            .HasForeignKey(a => a.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Address>()
            .HasOne(a => a.State)
            .WithMany()
            .HasForeignKey(a => a.StateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Company -> Address (N:1, AddressId nullable, SetNull al borrar Address)
        modelBuilder
            .Entity<Company>()
            .HasOne(c => c.Address)
            .WithMany()
            .HasForeignKey(c => c.AddressId)
            .OnDelete(DeleteBehavior.SetNull);

        // TaxUser -> Address (N:1, AddressId nullable, SetNull al borrar Address)
        modelBuilder
            .Entity<TaxUser>()
            .HasOne(u => u.Address)
            .WithMany()
            .HasForeignKey(u => u.AddressId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed data
        SeedPermissions(modelBuilder);
        SeedRoles(modelBuilder);
        SeedRolePermissions(modelBuilder);
        SeedCompanyAndDeveloper(modelBuilder);
        SeedGeography(modelBuilder);
    }

    // CONSTANTES CORREGIDAS
    static readonly Guid CompanySeedId = Guid.Parse("770e8400-e29b-41d4-a716-556655441000");
    static readonly Guid DevUserSeedId = Guid.Parse("880e8400-e29b-41d4-a716-556655441000");
    static readonly Guid DeveloperRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655441001");
    static readonly Guid AdministratorRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655441002");
    static readonly Guid UserRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655441003");
    static readonly Guid CustomerRoleId = Guid.Parse("550e8400-e29b-41d4-a716-446655441004");

    // Address seeds (GUID)
    static readonly Guid CompanyAddressSeedId = Guid.Parse("990e8400-e29b-41d4-a716-556655443001");
    static readonly Guid DevUserAddressSeedId = Guid.Parse("990e8400-e29b-41d4-a716-556655443000");

    // Geografía fija US
    const int USA = 220; // United States
    const int FL = 9; // Florida

    private static Guid NewPerm(int n) => Guid.Parse($"550e8400-e29b-41d4-a716-44665544{n:0000}");

    // PERMISOS
    private static void SeedPermissions(ModelBuilder modelBuilder)
    {
        // Permisos básicos (1-26)
        modelBuilder
            .Entity<Permission>()
            .HasData(
                new Permission
                {
                    Id = NewPerm(1),
                    Name = "Create Permissions",
                    Code = "Permission.Create",
                },
                new Permission
                {
                    Id = NewPerm(2),
                    Name = "Read Permissions",
                    Code = "Permission.Read",
                },
                new Permission
                {
                    Id = NewPerm(3),
                    Name = "View Permissions",
                    Code = "Permission.View",
                },
                new Permission
                {
                    Id = NewPerm(4),
                    Name = "Delete Permissions",
                    Code = "Permission.Delete",
                },
                new Permission
                {
                    Id = NewPerm(5),
                    Name = "Update Permissions",
                    Code = "Permission.Update",
                },
                new Permission
                {
                    Id = NewPerm(6),
                    Name = "Create TaxUsers",
                    Code = "TaxUser.Create",
                },
                new Permission
                {
                    Id = NewPerm(7),
                    Name = "Read TaxUsers",
                    Code = "TaxUser.Read",
                },
                new Permission
                {
                    Id = NewPerm(8),
                    Name = "View TaxUsers",
                    Code = "TaxUser.View",
                },
                new Permission
                {
                    Id = NewPerm(9),
                    Name = "Delete TaxUsers",
                    Code = "TaxUser.Delete",
                },
                new Permission
                {
                    Id = NewPerm(10),
                    Name = "Update TaxUsers",
                    Code = "TaxUser.Update",
                },
                new Permission
                {
                    Id = NewPerm(11),
                    Name = "Create Customers",
                    Code = "Customer.Create",
                },
                new Permission
                {
                    Id = NewPerm(12),
                    Name = "Read Customers",
                    Code = "Customer.Read",
                },
                new Permission
                {
                    Id = NewPerm(13),
                    Name = "View Customers",
                    Code = "Customer.View",
                },
                new Permission
                {
                    Id = NewPerm(14),
                    Name = "Delete Customers",
                    Code = "Customer.Delete",
                },
                new Permission
                {
                    Id = NewPerm(15),
                    Name = "Update Customers",
                    Code = "Customer.Update",
                },
                new Permission
                {
                    Id = NewPerm(16),
                    Name = "Create Roles",
                    Code = "Role.Create",
                },
                new Permission
                {
                    Id = NewPerm(17),
                    Name = "Read Roles",
                    Code = "Role.Read",
                },
                new Permission
                {
                    Id = NewPerm(18),
                    Name = "View Roles",
                    Code = "Role.View",
                },
                new Permission
                {
                    Id = NewPerm(19),
                    Name = "Delete Roles",
                    Code = "Role.Delete",
                },
                new Permission
                {
                    Id = NewPerm(20),
                    Name = "Update Roles",
                    Code = "Role.Update",
                },
                new Permission
                {
                    Id = NewPerm(21),
                    Name = "Create RolePermissions",
                    Code = "RolePermission.Create",
                },
                new Permission
                {
                    Id = NewPerm(22),
                    Name = "Read RolePermissions",
                    Code = "RolePermission.Read",
                },
                new Permission
                {
                    Id = NewPerm(23),
                    Name = "View RolePermissions",
                    Code = "RolePermission.View",
                },
                new Permission
                {
                    Id = NewPerm(24),
                    Name = "Delete RolePermissions",
                    Code = "RolePermission.Delete",
                },
                new Permission
                {
                    Id = NewPerm(25),
                    Name = "Update RolePermissions",
                    Code = "RolePermission.Update",
                },
                new Permission
                {
                    Id = NewPerm(26),
                    Name = "Read own profile",
                    Code = "Customer.SelfRead",
                }
            );

        // Permisos adicionales (27-39)
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
            (40, "Create Companies", "Company.Create"),
            (41, "Read Companies", "Company.Read"),
            (42, "View Companies", "Company.View"),
            (43, "Update Companies", "Company.Update"),
            (44, "Delete Companies", "Company.Delete"),
        }.Select(t => new Permission
        {
            Id = NewPerm(t.Item1),
            Name = t.Item2,
            Code = t.Item3,
        });

        modelBuilder.Entity<Permission>().HasData(extraPerms);
    }

    // ROLES
    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Role>()
            .HasData(
                new Role
                {
                    Id = DeveloperRoleId,
                    Name = "Developer",
                    Description =
                        "Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform.",
                    PortalAccess = PortalAccess.Developer,
                },
                new Role
                {
                    Id = AdministratorRoleId,
                    Name = "Administrator",
                    Description =
                        "Administrator of the company/preparer with total control of their organization.",
                    PortalAccess = PortalAccess.Staff,
                },
                new Role
                {
                    Id = UserRoleId,
                    Name = "User",
                    Description =
                        "User with limited access to specific functionalities of the company.",
                    PortalAccess = PortalAccess.Staff,
                },
                new Role
                {
                    Id = CustomerRoleId,
                    Name = "Customer",
                    Description =
                        "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality",
                    PortalAccess = PortalAccess.Customer,
                }
            );
    }

    // ROLE PERMISSIONS
    private static void SeedRolePermissions(ModelBuilder mb)
    {
        SeedDeveloperRolePermissions(mb);
        SeedAdministratorRolePermissions(mb);
        SeedUserRolePermissions(mb);
        SeedCustomerRolePermissions(mb);
    }

    // DEVELOPER - Todos los permisos (1-44)
    private static void SeedDeveloperRolePermissions(ModelBuilder mb)
    {
        var entries = Enumerable
            .Range(1, 44)
            .Select(i => new RolePermission
            {
                Id = Guid.Parse($"660e8400-e29b-41d4-a716-44665545{i:0000}"),
                RoleId = DeveloperRoleId,
                PermissionId = NewPerm(i),
            });

        mb.Entity<RolePermission>().HasData(entries);
    }

    // ADMINISTRATOR - Permisos de gestión de company
    private static void SeedAdministratorRolePermissions(ModelBuilder mb)
    {
        var adminPermissions = new[]
        {
            // Gestión de usuarios de su company
            "TaxUser.Create",
            "TaxUser.Read",
            "TaxUser.View",
            "TaxUser.Update",
            "TaxUser.Delete",
            // Gestión de customers
            "Customer.Create",
            "Customer.Read",
            "Customer.View",
            "Customer.Update",
            "Customer.DisableLogin",
            "Customer.EnableLogin",
            // Gestión de dependientes
            "Dependent.Create",
            "Dependent.Update",
            "Dependent.Delete",
            "Dependent.Read",
            "Dependent.Viewer",
            // Gestión de información fiscal
            "TaxInformation.Create",
            "TaxInformation.Update",
            "TaxInformation.Delete",
            "TaxInformation.Read",
            "TaxInformation.Viewer",
            // Ver roles y permisos
            "Role.View",
            "Permission.View",
            // Gestión de su company
            "Company.Read",
            "Company.View",
            "Company.Update",
            // Ver sesiones
            "Sessions.Read",
        };

        var rolePermissions = adminPermissions.Select(
            (code, idx) =>
                new RolePermission
                {
                    Id = Guid.Parse($"770e8400-e29b-41d4-a716-55665546{idx:0000}"),
                    RoleId = AdministratorRoleId,
                    PermissionId = GetPermissionIdByCode(code),
                }
        );

        mb.Entity<RolePermission>().HasData(rolePermissions);
    }

    // USER - Permisos básicos de lectura
    private static void SeedUserRolePermissions(ModelBuilder mb)
    {
        var userPermissions = new[]
        {
            "Customer.View",
            "Customer.Read",
            "Dependent.Viewer",
            "Dependent.Read",
            "TaxInformation.Viewer",
            "TaxInformation.Read",
            "TaxUser.View",
            "Role.View",
            "Permission.View",
            "Sessions.Read",
            "Customer.SelfRead",
        };

        var rolePermissions = userPermissions.Select(
            (code, idx) =>
                new RolePermission
                {
                    Id = Guid.Parse($"880e8400-e29b-41d4-a716-55665547{idx:0000}"),
                    RoleId = UserRoleId,
                    PermissionId = GetPermissionIdByCode(code),
                }
        );

        mb.Entity<RolePermission>().HasData(rolePermissions);
    }

    // CUSTOMER - Solo lectura de su perfil
    private static void SeedCustomerRolePermissions(ModelBuilder mb)
    {
        mb.Entity<RolePermission>()
            .HasData(
                new RolePermission
                {
                    Id = Guid.Parse("770e8400-e29b-41d4-a716-556655450026"),
                    RoleId = CustomerRoleId,
                    PermissionId = NewPerm(26), // Customer.SelfRead
                }
            );
    }

    // Helper para obtener ID de permiso por código
    private static Guid GetPermissionIdByCode(string code)
    {
        var permissionMapping = new Dictionary<string, int>
        {
            ["Permission.Create"] = 1,
            ["Permission.Read"] = 2,
            ["Permission.View"] = 3,
            ["Permission.Delete"] = 4,
            ["Permission.Update"] = 5,
            ["TaxUser.Create"] = 6,
            ["TaxUser.Read"] = 7,
            ["TaxUser.View"] = 8,
            ["TaxUser.Delete"] = 9,
            ["TaxUser.Update"] = 10,
            ["Customer.Create"] = 11,
            ["Customer.Read"] = 12,
            ["Customer.View"] = 13,
            ["Customer.Delete"] = 14,
            ["Customer.Update"] = 15,
            ["Role.Create"] = 16,
            ["Role.Read"] = 17,
            ["Role.View"] = 18,
            ["Role.Delete"] = 19,
            ["Role.Update"] = 20,
            ["RolePermission.Create"] = 21,
            ["RolePermission.Read"] = 22,
            ["RolePermission.View"] = 23,
            ["RolePermission.Delete"] = 24,
            ["RolePermission.Update"] = 25,
            ["Customer.SelfRead"] = 26,
            ["Customer.DisableLogin"] = 27,
            ["Customer.EnableLogin"] = 28,
            ["Sessions.Read"] = 29,
            ["Dependent.Create"] = 30,
            ["Dependent.Update"] = 31,
            ["Dependent.Delete"] = 32,
            ["Dependent.Read"] = 33,
            ["Dependent.Viewer"] = 34,
            ["TaxInformation.Create"] = 35,
            ["TaxInformation.Update"] = 36,
            ["TaxInformation.Delete"] = 37,
            ["TaxInformation.Read"] = 38,
            ["TaxInformation.Viewer"] = 39,
            ["Company.Create"] = 40,
            ["Company.Read"] = 41,
            ["Company.View"] = 42,
            ["Company.Update"] = 43,
            ["Company.Delete"] = 44,
        };

        if (permissionMapping.TryGetValue(code, out var permNumber))
            return NewPerm(permNumber);

        throw new ArgumentException($"Permission code '{code}' not found");
    }

    // =======================
    // === GEOGRAPHY SEED ====
    // =======================
    private static void SeedGeography(ModelBuilder modelBuilder)
    {
        // País
        modelBuilder
            .Entity<Country>()
            .HasData(
                new Country { Id = 1, Name = "Afganistán" },
                new Country { Id = 2, Name = "Albania" },
                new Country { Id = 3, Name = "Algeria" },
                new Country { Id = 4, Name = "Samoa Americana" },
                new Country { Id = 5, Name = "Andorra" },
                new Country { Id = 6, Name = "Angola" },
                new Country { Id = 7, Name = "Anguilla" },
                new Country { Id = 8, Name = "Antártida" },
                new Country { Id = 9, Name = "Antigua y Barbuda" },
                new Country { Id = 10, Name = "Argentina" },
                new Country { Id = 11, Name = "Armenia" },
                new Country { Id = 12, Name = "Aruba" },
                new Country { Id = 13, Name = "Australia" },
                new Country { Id = 14, Name = "Austria" },
                new Country { Id = 15, Name = "Azerbaiyán" },
                new Country { Id = 16, Name = "Bahamas" },
                new Country { Id = 17, Name = "Bahrein" },
                new Country { Id = 18, Name = "Bangladesh" },
                new Country { Id = 19, Name = "Barbados" },
                new Country { Id = 20, Name = "Bielorrusia" },
                new Country { Id = 21, Name = "Bélgica" },
                new Country { Id = 22, Name = "Belice" },
                new Country { Id = 23, Name = "Benín" },
                new Country { Id = 24, Name = "Bermuda" },
                new Country { Id = 25, Name = "Bután" },
                new Country { Id = 26, Name = "Bolivia" },
                new Country { Id = 27, Name = "Bosnia-Herzegovina" },
                new Country { Id = 28, Name = "Botswana" },
                new Country { Id = 29, Name = "Brasil" },
                new Country { Id = 30, Name = "Brunei" },
                new Country { Id = 31, Name = "Bulgaria" },
                new Country { Id = 32, Name = "Burkina Faso" },
                new Country { Id = 33, Name = "Burundi" },
                new Country { Id = 34, Name = "Camboya" },
                new Country { Id = 35, Name = "Camerún" },
                new Country { Id = 36, Name = "Canadá" },
                new Country { Id = 37, Name = "Cabo Verde" },
                new Country { Id = 38, Name = "Islas Caimán" },
                new Country { Id = 39, Name = "República Centroafricana" },
                new Country { Id = 40, Name = "Chad" },
                new Country { Id = 41, Name = "Chile" },
                new Country { Id = 42, Name = "China" },
                new Country { Id = 43, Name = "Isla de Navidad" },
                new Country { Id = 44, Name = "Islas Cocos" },
                new Country { Id = 45, Name = "Colombia" },
                new Country { Id = 46, Name = "Comores" },
                new Country { Id = 47, Name = "República del Congo" },
                new Country { Id = 48, Name = "República Democrática del Congo" },
                new Country { Id = 49, Name = "Islas Cook" },
                new Country { Id = 50, Name = "Costa Rica" },
                new Country { Id = 51, Name = "Costa de Marfíl" },
                new Country { Id = 52, Name = "Croacia" },
                new Country { Id = 53, Name = "Cuba" },
                new Country { Id = 54, Name = "Chipre" },
                new Country { Id = 55, Name = "República Checa" },
                new Country { Id = 56, Name = "Dinamarca" },
                new Country { Id = 57, Name = "Djibouti" },
                new Country { Id = 58, Name = "Dominica" },
                new Country { Id = 59, Name = "República Dominicana" },
                new Country { Id = 60, Name = "Ecuador" },
                new Country { Id = 61, Name = "Egipto" },
                new Country { Id = 62, Name = "El Salvador" },
                new Country { Id = 63, Name = "Guinea Ecuatorial" },
                new Country { Id = 64, Name = "Eritrea" },
                new Country { Id = 65, Name = "Estonia" },
                new Country { Id = 66, Name = "Etiopía" },
                new Country { Id = 67, Name = "Islas Malvinas" },
                new Country { Id = 68, Name = "Islas Feroe" },
                new Country { Id = 69, Name = "Fiji" },
                new Country { Id = 70, Name = "Finlandia" },
                new Country { Id = 71, Name = "Francia" },
                new Country { Id = 72, Name = "Guyana Francesa" },
                new Country { Id = 73, Name = "Polinesia Francesa" },
                new Country { Id = 74, Name = "Tierras Australes y Antárticas Francesas" },
                new Country { Id = 75, Name = "Gabón" },
                new Country { Id = 76, Name = "Gambia" },
                new Country { Id = 77, Name = "Georgia" },
                new Country { Id = 78, Name = "Alemania" },
                new Country { Id = 79, Name = "Ghana" },
                new Country { Id = 80, Name = "Gibraltar" },
                new Country { Id = 81, Name = "Grecia" },
                new Country { Id = 82, Name = "Groenlandia" },
                new Country { Id = 83, Name = "Granada" },
                new Country { Id = 84, Name = "Guadalupe" },
                new Country { Id = 85, Name = "Guam" },
                new Country { Id = 86, Name = "Guatemala" },
                new Country { Id = 87, Name = "Guinea" },
                new Country { Id = 88, Name = "Guinea-Bissau" },
                new Country { Id = 89, Name = "Guyana" },
                new Country { Id = 90, Name = "Haití" },
                new Country { Id = 91, Name = "Vaticano" },
                new Country { Id = 92, Name = "Honduras" },
                new Country { Id = 93, Name = "Hong Kong" },
                new Country { Id = 94, Name = "Hungría" },
                new Country { Id = 95, Name = "Islandia" },
                new Country { Id = 96, Name = "India" },
                new Country { Id = 97, Name = "Indonesia" },
                new Country { Id = 98, Name = "Irán" },
                new Country { Id = 99, Name = "Iraq" },
                new Country { Id = 100, Name = "Irlanda" },
                new Country { Id = 101, Name = "Israel" },
                new Country { Id = 102, Name = "Italia" },
                new Country { Id = 103, Name = "Jamaica" },
                new Country { Id = 104, Name = "Japón" },
                new Country { Id = 105, Name = "Jordania" },
                new Country { Id = 106, Name = "Kazajstán" },
                new Country { Id = 107, Name = "Kenia" },
                new Country { Id = 108, Name = "Kiribati" },
                new Country { Id = 109, Name = "Corea del Norte" },
                new Country { Id = 110, Name = "Corea del Sur" },
                new Country { Id = 111, Name = "Kuwait" },
                new Country { Id = 112, Name = "Kirguistán" },
                new Country { Id = 113, Name = "Laos" },
                new Country { Id = 114, Name = "Letonia" },
                new Country { Id = 115, Name = "Líbano" },
                new Country { Id = 116, Name = "Lesotho" },
                new Country { Id = 117, Name = "Liberia" },
                new Country { Id = 118, Name = "Libia" },
                new Country { Id = 119, Name = "Liechtenstein" },
                new Country { Id = 120, Name = "Lituania" },
                new Country { Id = 121, Name = "Luxemburgo" },
                new Country { Id = 122, Name = "Macao" },
                new Country { Id = 123, Name = "Macedonia" },
                new Country { Id = 124, Name = "Madagascar" },
                new Country { Id = 125, Name = "Malawi" },
                new Country { Id = 126, Name = "Malasia" },
                new Country { Id = 127, Name = "Maldivas" },
                new Country { Id = 128, Name = "Mali" },
                new Country { Id = 129, Name = "Malta" },
                new Country { Id = 130, Name = "Islas Marshall" },
                new Country { Id = 131, Name = "Martinica" },
                new Country { Id = 132, Name = "Mauritania" },
                new Country { Id = 133, Name = "Mauricio" },
                new Country { Id = 134, Name = "Mayotte" },
                new Country { Id = 135, Name = "México" },
                new Country { Id = 136, Name = "Estados Federados de Micronesia" },
                new Country { Id = 137, Name = "Moldavia" },
                new Country { Id = 138, Name = "Mónaco" },
                new Country { Id = 139, Name = "Mongolia" },
                new Country { Id = 140, Name = "Montserrat" },
                new Country { Id = 141, Name = "Marruecos" },
                new Country { Id = 142, Name = "Mozambique" },
                new Country { Id = 143, Name = "Myanmar" },
                new Country { Id = 144, Name = "Namibia" },
                new Country { Id = 145, Name = "Nauru" },
                new Country { Id = 146, Name = "Nepal" },
                new Country { Id = 147, Name = "Holanda" },
                new Country { Id = 148, Name = "Antillas Holandesas" },
                new Country { Id = 149, Name = "Nueva Caledonia" },
                new Country { Id = 150, Name = "Nueva Zelanda" },
                new Country { Id = 151, Name = "Nicaragua" },
                new Country { Id = 152, Name = "Niger" },
                new Country { Id = 153, Name = "Nigeria" },
                new Country { Id = 154, Name = "Niue" },
                new Country { Id = 155, Name = "Islas Norfolk" },
                new Country { Id = 156, Name = "Islas Marianas del Norte" },
                new Country { Id = 157, Name = "Noruega" },
                new Country { Id = 158, Name = "Omán" },
                new Country { Id = 159, Name = "Pakistán" },
                new Country { Id = 160, Name = "Palau" },
                new Country { Id = 161, Name = "Palestina" },
                new Country { Id = 162, Name = "Panamá" },
                new Country { Id = 163, Name = "Papua Nueva Guinea" },
                new Country { Id = 164, Name = "Paraguay" },
                new Country { Id = 165, Name = "Perú" },
                new Country { Id = 166, Name = "Filipinas" },
                new Country { Id = 167, Name = "Pitcairn" },
                new Country { Id = 168, Name = "Polonia" },
                new Country { Id = 169, Name = "Portugal" },
                new Country { Id = 170, Name = "Puerto Rico" },
                new Country { Id = 171, Name = "Qatar" },
                new Country { Id = 172, Name = "Reunión" },
                new Country { Id = 173, Name = "Rumanía" },
                new Country { Id = 174, Name = "Rusia" },
                new Country { Id = 175, Name = "Ruanda" },
                new Country { Id = 176, Name = "Santa Helena" },
                new Country { Id = 177, Name = "San Kitts y Nevis" },
                new Country { Id = 178, Name = "Santa Lucía" },
                new Country { Id = 179, Name = "San Vicente y Granadinas" },
                new Country { Id = 180, Name = "Samoa" },
                new Country { Id = 181, Name = "San Marino" },
                new Country { Id = 182, Name = "Santo Tomé y Príncipe" },
                new Country { Id = 183, Name = "Arabia Saudita" },
                new Country { Id = 184, Name = "Senegal" },
                new Country { Id = 185, Name = "Serbia" },
                new Country { Id = 186, Name = "Seychelles" },
                new Country { Id = 187, Name = "Sierra Leona" },
                new Country { Id = 188, Name = "Singapur" },
                new Country { Id = 189, Name = "Eslovaquía" },
                new Country { Id = 190, Name = "Eslovenia" },
                new Country { Id = 191, Name = "Islas Salomón" },
                new Country { Id = 192, Name = "Somalia" },
                new Country { Id = 193, Name = "Sudáfrica" },
                new Country { Id = 194, Name = "España" },
                new Country { Id = 195, Name = "Sri Lanka" },
                new Country { Id = 196, Name = "Sudán" },
                new Country { Id = 197, Name = "Surinam" },
                new Country { Id = 198, Name = "Swazilandia" },
                new Country { Id = 199, Name = "Suecia" },
                new Country { Id = 200, Name = "Suiza" },
                new Country { Id = 201, Name = "Siria" },
                new Country { Id = 202, Name = "Taiwán" },
                new Country { Id = 203, Name = "Tadjikistan" },
                new Country { Id = 204, Name = "Tanzania" },
                new Country { Id = 205, Name = "Tailandia" },
                new Country { Id = 206, Name = "Timor Oriental" },
                new Country { Id = 207, Name = "Togo" },
                new Country { Id = 208, Name = "Tokelau" },
                new Country { Id = 209, Name = "Tonga" },
                new Country { Id = 210, Name = "Trinidad y Tobago" },
                new Country { Id = 211, Name = "Túnez" },
                new Country { Id = 212, Name = "Turquía" },
                new Country { Id = 213, Name = "Turkmenistan" },
                new Country { Id = 214, Name = "Islas Turcas y Caicos" },
                new Country { Id = 215, Name = "Tuvalu" },
                new Country { Id = 216, Name = "Uganda" },
                new Country { Id = 217, Name = "Ucrania" },
                new Country { Id = 218, Name = "Emiratos Árabes Unidos" },
                new Country { Id = 219, Name = "Reino Unido" },
                new Country { Id = 220, Name = "Estados Unidos" },
                new Country { Id = 221, Name = "Uruguay" },
                new Country { Id = 222, Name = "Uzbekistán" },
                new Country { Id = 223, Name = "Vanuatu" },
                new Country { Id = 224, Name = "Venezuela" },
                new Country { Id = 225, Name = "Vietnam" },
                new Country { Id = 226, Name = "Islas Vírgenes Británicas" },
                new Country { Id = 227, Name = "Islas Vírgenes Americanas" },
                new Country { Id = 228, Name = "Wallis y Futuna" },
                new Country { Id = 229, Name = "Sáhara Occidental" },
                new Country { Id = 230, Name = "Yemen" },
                new Country { Id = 231, Name = "Zambia" },
                new Country { Id = 232, Name = "Zimbabwe" }
            );

        // Estados (1..51)
        modelBuilder
            .Entity<State>()
            .HasData(
                new State
                {
                    Id = 1,
                    Name = "Alabama",
                    CountryId = USA,
                },
                new State
                {
                    Id = 2,
                    Name = "Alaska",
                    CountryId = USA,
                },
                new State
                {
                    Id = 3,
                    Name = "Arizona",
                    CountryId = USA,
                },
                new State
                {
                    Id = 4,
                    Name = "Arkansas",
                    CountryId = USA,
                },
                new State
                {
                    Id = 5,
                    Name = "California",
                    CountryId = USA,
                },
                new State
                {
                    Id = 6,
                    Name = "Colorado",
                    CountryId = USA,
                },
                new State
                {
                    Id = 7,
                    Name = "Connecticut",
                    CountryId = USA,
                },
                new State
                {
                    Id = 8,
                    Name = "Delaware",
                    CountryId = USA,
                },
                new State
                {
                    Id = 9,
                    Name = "Florida",
                    CountryId = USA,
                },
                new State
                {
                    Id = 10,
                    Name = "Georgia",
                    CountryId = USA,
                },
                new State
                {
                    Id = 11,
                    Name = "Hawaii",
                    CountryId = USA,
                },
                new State
                {
                    Id = 12,
                    Name = "Idaho",
                    CountryId = USA,
                },
                new State
                {
                    Id = 13,
                    Name = "Illinois",
                    CountryId = USA,
                },
                new State
                {
                    Id = 14,
                    Name = "Indiana",
                    CountryId = USA,
                },
                new State
                {
                    Id = 15,
                    Name = "Iowa",
                    CountryId = USA,
                },
                new State
                {
                    Id = 16,
                    Name = "Kansas",
                    CountryId = USA,
                },
                new State
                {
                    Id = 17,
                    Name = "Kentucky",
                    CountryId = USA,
                },
                new State
                {
                    Id = 18,
                    Name = "Louisiana",
                    CountryId = USA,
                },
                new State
                {
                    Id = 19,
                    Name = "Maine",
                    CountryId = USA,
                },
                new State
                {
                    Id = 20,
                    Name = "Maryland",
                    CountryId = USA,
                },
                new State
                {
                    Id = 21,
                    Name = "Massachusetts",
                    CountryId = USA,
                },
                new State
                {
                    Id = 22,
                    Name = "Michigan",
                    CountryId = USA,
                },
                new State
                {
                    Id = 23,
                    Name = "Minnesota",
                    CountryId = USA,
                },
                new State
                {
                    Id = 24,
                    Name = "Mississippi",
                    CountryId = USA,
                },
                new State
                {
                    Id = 25,
                    Name = "Missouri",
                    CountryId = USA,
                },
                new State
                {
                    Id = 26,
                    Name = "Montana",
                    CountryId = USA,
                },
                new State
                {
                    Id = 27,
                    Name = "Nebraska",
                    CountryId = USA,
                },
                new State
                {
                    Id = 28,
                    Name = "Nevada",
                    CountryId = USA,
                },
                new State
                {
                    Id = 29,
                    Name = "New Hampshire",
                    CountryId = USA,
                },
                new State
                {
                    Id = 30,
                    Name = "New Jersey",
                    CountryId = USA,
                },
                new State
                {
                    Id = 31,
                    Name = "New Mexico",
                    CountryId = USA,
                },
                new State
                {
                    Id = 32,
                    Name = "New York",
                    CountryId = USA,
                },
                new State
                {
                    Id = 33,
                    Name = "North Carolina",
                    CountryId = USA,
                },
                new State
                {
                    Id = 34,
                    Name = "North Dakota",
                    CountryId = USA,
                },
                new State
                {
                    Id = 35,
                    Name = "Ohio",
                    CountryId = USA,
                },
                new State
                {
                    Id = 36,
                    Name = "Oklahoma",
                    CountryId = USA,
                },
                new State
                {
                    Id = 37,
                    Name = "Oregon",
                    CountryId = USA,
                },
                new State
                {
                    Id = 38,
                    Name = "Pennsylvania",
                    CountryId = USA,
                },
                new State
                {
                    Id = 39,
                    Name = "Rhode Island",
                    CountryId = USA,
                },
                new State
                {
                    Id = 40,
                    Name = "South Carolina",
                    CountryId = USA,
                },
                new State
                {
                    Id = 41,
                    Name = "South Dakota",
                    CountryId = USA,
                },
                new State
                {
                    Id = 42,
                    Name = "Tennessee",
                    CountryId = USA,
                },
                new State
                {
                    Id = 43,
                    Name = "Texas",
                    CountryId = USA,
                },
                new State
                {
                    Id = 44,
                    Name = "Utah",
                    CountryId = USA,
                },
                new State
                {
                    Id = 45,
                    Name = "Vermont",
                    CountryId = USA,
                },
                new State
                {
                    Id = 46,
                    Name = "Virginia",
                    CountryId = USA,
                },
                new State
                {
                    Id = 47,
                    Name = "Washington",
                    CountryId = USA,
                },
                new State
                {
                    Id = 48,
                    Name = "West Virginia",
                    CountryId = USA,
                },
                new State
                {
                    Id = 49,
                    Name = "Wisconsin",
                    CountryId = USA,
                },
                new State
                {
                    Id = 50,
                    Name = "Wyoming",
                    CountryId = USA,
                },
                new State
                {
                    Id = 51,
                    Name = "District of Columbia",
                    CountryId = USA,
                }
            );
    }

    // COMPANY Y DEVELOPER SEED
    private static void SeedCompanyAndDeveloper(ModelBuilder mb)
    {
        // 1) Address para la Company (ejemplo en Miami, FL)
        mb.Entity<Address>()
            .HasData(
                new Address
                {
                    Id = CompanyAddressSeedId,
                    CountryId = USA,
                    StateId = FL, // Florida
                    City = "Miami",
                    Street = "NW 1st Ave 100",
                    Line = null,
                    ZipCode = "33101",
                }
            );

        // 2) Address para el usuario Developer
        mb.Entity<Address>()
            .HasData(
                new Address
                {
                    Id = DevUserAddressSeedId,
                    CountryId = USA,
                    StateId = FL, // Florida
                    City = "Miami",
                    Street = "NW 2nd Ave 200",
                    Line = "Suite 5",
                    ZipCode = "33101",
                }
            );

        // 3) Company StackVision
        mb.Entity<Company>()
            .HasData(
                new Company
                {
                    Id = CompanySeedId,
                    IsCompany = true,
                    FullName = null,
                    CompanyName = "StackVision Software S.R.L.",
                    Phone = "8298981594",
                    Description = "Software Developers Assembly.",
                    Domain = "stackvision",
                    UserLimit = 25,
                    Brand = "https://images5.example.com/",
                    AddressId = CompanyAddressSeedId,
                }
            );

        // 4) Usuario Developer (se crea automáticamente)
        mb.Entity<TaxUser>()
            .HasData(
                new
                {
                    Id = DevUserSeedId,
                    CompanyId = CompanySeedId,
                    Email = "stackvisionsoftware@gmail.com",
                    Password = "zBLVJHyDUQKSp3ZYdgIeOEDnoeD61Zg566QoP2165AQAPHxzvJlAWjt1dV+Qinc7",
                    IsActive = true,
                    Name = "Developer",
                    LastName = "StackVision",
                    PhoneNumber = "8298981594",
                    PhotoUrl = (string?)null,
                    Confirm = true,
                    ConfirmToken = (string?)null,
                    ResetPasswordToken = (string?)null,
                    ResetPasswordExpires = (DateTime?)null,
                    Factor2 = (bool?)null,
                    Otp = (string?)null,
                    OtpVerified = false,
                    OtpExpires = (DateTime?)null,
                    AddressId = DevUserAddressSeedId,
                }
            );

        // UserRole - Developer
        mb.Entity<UserRole>()
            .HasData(
                new UserRole
                {
                    Id = Guid.Parse("880e8400-e29b-41d4-a716-556655442000"),
                    TaxUserId = DevUserSeedId,
                    RoleId = DeveloperRoleId,
                }
            );
    }
}
