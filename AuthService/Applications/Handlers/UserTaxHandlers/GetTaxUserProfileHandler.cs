using Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserHandlers;

public class GetTaxUserProfileHandler
    : IRequestHandler<GetTaxUserProfileQuery, ApiResponse<UserProfileDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetTaxUserProfileHandler> _logger;

    public GetTaxUserProfileHandler(
        ApplicationDbContext db,
        ILogger<GetTaxUserProfileHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<UserProfileDTO>> Handle(
        GetTaxUserProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 🔍 PASO 1: Buscar TaxUser con información completa
            var profile = await GetTaxUserProfileAsync(request.UserId, cancellationToken);

            if (profile == null)
            {
                _logger.LogWarning("TaxUser not found: {UserId}", request.UserId);
                return new ApiResponse<UserProfileDTO>(false, "User not found", null!);
            }

            _logger.LogInformation(
                "User profile retrieved successfully: {UserId} (IsOwner: {IsOwner})",
                request.UserId,
                profile.IsOwner
            );

            return new ApiResponse<UserProfileDTO>(
                true,
                "User profile retrieved successfully",
                profile
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error reading profile for {UserId}: {Message}",
                request.UserId,
                ex.Message
            );
            return new ApiResponse<UserProfileDTO>(false, "Internal error", null!);
        }
    }

    /// <summary>
    /// Obtiene perfil completo de TaxUser con información de Company y permisos efectivos
    /// </summary>
    private async Task<UserProfileDTO?> GetTaxUserProfileAsync(Guid userId, CancellationToken ct)
    {
        // Query principal para obtener información del TaxUser y Company
        var userQuery =
            from u in _db.TaxUsers
            join c in _db.Companies on u.CompanyId equals c.Id
            join cp in _db.CustomPlans on c.CustomPlanId equals cp.Id
            join a in _db.Addresses on u.AddressId equals a.Id into addresses
            from a in addresses.DefaultIfEmpty()
            join country in _db.Countries on a.CountryId equals country.Id into countries
            from country in countries.DefaultIfEmpty()
            join state in _db.States on a.StateId equals state.Id into states
            from state in states.DefaultIfEmpty()
            join ca in _db.Addresses on c.AddressId equals ca.Id into companyAddresses
            from ca in companyAddresses.DefaultIfEmpty()
            join ccountry in _db.Countries on ca.CountryId equals ccountry.Id into companyCountries
            from ccountry in companyCountries.DefaultIfEmpty()
            join cstate in _db.States on ca.StateId equals cstate.Id into companyStates
            from cstate in companyStates.DefaultIfEmpty()
            where u.Id == userId
            select new UserProfileDTO
            {
                Id = u.Id,
                CompanyId = u.CompanyId,
                Email = u.Email,
                IsOwner = u.IsOwner, // NUEVO: Campo IsOwner
                Name = u.Name,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                PhotoUrl = u.PhotoUrl,

                // Dirección del usuario
                Address =
                    a != null
                        ? new AddressDTO
                        {
                            CountryId = a.CountryId,
                            StateId = a.StateId,
                            City = a.City,
                            Street = a.Street,
                            Line = a.Line,
                            ZipCode = a.ZipCode,
                            CountryName = country.Name,
                            StateName = state.Name,
                        }
                        : null,

                // Información de la Company
                CompanyFullName = c.FullName,
                CompanyName = c.CompanyName,
                CompanyBrand = c.Brand,
                CompanyIsIndividual = !c.IsCompany,
                CompanyDomain = c.Domain,

                // Dirección de la Company
                CompanyAddress =
                    ca != null
                        ? new AddressDTO
                        {
                            CountryId = ca.CountryId,
                            StateId = ca.StateId,
                            City = ca.City,
                            Street = ca.Street,
                            Line = ca.Line,
                            ZipCode = ca.ZipCode,
                            CountryName = ccountry.Name,
                            StateName = cstate.Name,
                        }
                        : null,

                // NUEVO: Información del CustomPlan
                CustomPlanId = cp.Id,
                CustomPlanPrice = cp.Price,
                CustomPlanIsActive = cp.IsActive,

                // Inicializar colecciones
                RoleNames = new List<string>(),
                AdditionalModules = new List<string>(),
                EffectivePermissions = new List<string>(),
            };

        var user = await userQuery.FirstOrDefaultAsync(ct);
        if (user == null)
            return null;

        // PASO 2: Obtener roles del TaxUser
        var rolesQuery =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.TaxUserId == userId
            select r.Name;

        user.RoleNames = await rolesQuery.ToListAsync(ct);

        // PASO 3: Obtener módulos adicionales del CustomPlan
        var additionalModulesQuery =
            from cp in _db.CustomPlans
            join cm in _db.CustomModules on cp.Id equals cm.CustomPlanId
            join m in _db.Modules on cm.ModuleId equals m.Id
            where cp.CompanyId == user.CompanyId && cm.IsIncluded && m.ServiceId == null // Solo módulos adicionales
            select m.Name;

        user.AdditionalModules = await additionalModulesQuery.ToListAsync(ct);

        // PASO 4: Calcular permisos efectivos
        user.EffectivePermissions = await GetEffectivePermissionsAsync(userId, ct);

        return user;
    }

    /// <summary>
    /// Calcula los permisos efectivos del TaxUser:
    /// (Permisos de roles + Permisos custom granted) - Permisos custom revoked
    /// </summary>
    private async Task<List<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct)
    {
        // Permisos base de los roles del usuario
        var rolePermissions = await (
            from ur in _db.UserRoles
            join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where ur.TaxUserId == userId && p.IsGranted // Solo permisos activos globalmente
            select p.Code
        ).ToListAsync(ct);

        // Permisos personalizados granted
        var customPermissionsGranted = await (
            from cp in _db.CompanyPermissions
            join p in _db.Permissions on cp.PermissionId equals p.Id
            where cp.TaxUserId == userId && cp.IsGranted && p.IsGranted
            select p.Code
        ).ToListAsync(ct);

        // Permisos personalizados revoked
        var customPermissionsRevoked = await (
            from cp in _db.CompanyPermissions
            join p in _db.Permissions on cp.PermissionId equals p.Id
            where cp.TaxUserId == userId && !cp.IsGranted
            select p.Code
        ).ToListAsync(ct);

        // Combinar permisos: (roles + custom granted) - revoked
        var effectivePermissions = rolePermissions
            .Concat(customPermissionsGranted)
            .Where(code => !customPermissionsRevoked.Contains(code))
            .Distinct()
            .OrderBy(code => code)
            .ToList();

        _logger.LogDebug(
            "Calculated effective permissions for TaxUser {UserId}: {PermissionCount} permissions",
            userId,
            effectivePermissions.Count
        );

        return effectivePermissions;
    }
}
