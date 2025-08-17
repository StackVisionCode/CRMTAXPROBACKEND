using AuthService.DTOs.CompanyDTOs;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetMyCompanyUsersHandler
    : IRequestHandler<GetMyCompanyUsersQuery, ApiResponse<CompanyUsersCompleteDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetMyCompanyUsersHandler> _logger;

    public GetMyCompanyUsersHandler(
        ApplicationDbContext dbContext,
        ILogger<GetMyCompanyUsersHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyUsersCompleteDTO>> Handle(
        GetMyCompanyUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Verificar que la company existe y obtener información del plan
            var companyInfoQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.CompanyId
                select new { Company = c, CustomPlan = cp };

            var companyInfo = await companyInfoQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyInfo?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyId);
                return new ApiResponse<CompanyUsersCompleteDTO>(
                    false,
                    "Company not found",
                    new CompanyUsersCompleteDTO()
                );
            }

            // 2. Obtener todos los TaxUsers de la company (sin cambios en la query principal)
            var usersQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                join a in _dbContext.Addresses on u.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _dbContext.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
                join ca in _dbContext.Addresses on c.AddressId equals ca.Id into companyAddresses
                from ca in companyAddresses.DefaultIfEmpty()
                join ccountry in _dbContext.Countries
                    on ca.CountryId equals ccountry.Id
                    into companyCountries
                from ccountry in companyCountries.DefaultIfEmpty()
                join cstate in _dbContext.States on ca.StateId equals cstate.Id into companyStates
                from cstate in companyStates.DefaultIfEmpty()
                where u.CompanyId == request.CompanyId
                orderby u.IsOwner descending, u.CreatedAt descending // Owner primero
                select new UserGetDTO
                {
                    Id = u.Id,
                    CompanyId = u.CompanyId,
                    Email = u.Email,
                    IsOwner = u.IsOwner,
                    Name = u.Name,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    PhotoUrl = u.PhotoUrl,
                    IsActive = u.IsActive,
                    Confirm = u.Confirm ?? false,
                    CreatedAt = u.CreatedAt,

                    // Direcciones (sin cambios)
                    Address =
                        a != null
                            ? new Applications.DTOs.CompanyDTOs.AddressDTO
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

                    // Información de la company (sin cambios)
                    CompanyFullName = c.FullName,
                    CompanyName = c.CompanyName,
                    CompanyBrand = c.Brand,
                    CompanyIsIndividual = !c.IsCompany,
                    CompanyDomain = c.Domain,

                    CompanyAddress =
                        ca != null
                            ? new Applications.DTOs.CompanyDTOs.AddressDTO
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

                    RoleNames = new List<string>(),
                    CustomPermissions = new List<string>(),
                };

            var users = await usersQuery.ToListAsync(cancellationToken);

            if (!users.Any())
            {
                _logger.LogInformation(
                    "No users found for company: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<CompanyUsersCompleteDTO>(
                    true,
                    "No users found for this company",
                    new CompanyUsersCompleteDTO
                    {
                        CompanyId = request.CompanyId,
                        CompanyName = companyInfo.Company.CompanyName,
                        IsCompany = companyInfo.Company.IsCompany,
                        ServiceUserLimit = companyInfo.CustomPlan.UserLimit,
                        Users = new List<UserGetDTO>(),
                    }
                );
            }

            // 3. Obtener roles y permisos (sin cambios)
            await PopulateTaxUserRolesAndPermissionsAsync(users, cancellationToken);

            // 🔧 4. CAMBIO: Usar CustomPlan.UserLimit
            var result = new CompanyUsersCompleteDTO
            {
                CompanyId = request.CompanyId,
                CompanyName = companyInfo.Company.IsCompany
                    ? companyInfo.Company.CompanyName
                    : companyInfo.Company.FullName,
                IsCompany = companyInfo.Company.IsCompany,
                ServiceUserLimit = companyInfo.CustomPlan.UserLimit,
                Users = users,
            };

            _logger.LogInformation(
                "Retrieved {Count} TaxUsers for company {CompanyId} (Owner: {Owner}, Regular: {Regular}). CustomPlan limit: {Limit}",
                result.TotalUsers,
                request.CompanyId,
                result.OwnerCount,
                result.RegularUsersCount,
                companyInfo.CustomPlan.UserLimit
            );

            return new ApiResponse<CompanyUsersCompleteDTO>(
                true,
                $"Company users retrieved successfully. {result.TotalUsers} users found.",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving users for company {CompanyId}: {Message}",
                request.CompanyId,
                ex.Message
            );
            return new ApiResponse<CompanyUsersCompleteDTO>(
                false,
                "Error retrieving company users",
                new CompanyUsersCompleteDTO()
            );
        }
    }

    /// <summary>
    /// Popula roles y permisos personalizados para TaxUsers
    /// </summary>
    private async Task PopulateTaxUserRolesAndPermissionsAsync(
        List<UserGetDTO> users,
        CancellationToken ct
    )
    {
        var userIds = users.Select(u => u.Id).ToList();

        // Obtener roles de los TaxUsers
        var rolesQuery =
            from ur in _dbContext.UserRoles
            join r in _dbContext.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.TaxUserId)
            select new { ur.TaxUserId, r.Name };

        var userRoles = await rolesQuery.ToListAsync(ct);
        var rolesByUser = userRoles
            .GroupBy(x => x.TaxUserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        // CompanyPermissions ahora apunta directamente a TaxUser
        var permissionsQuery =
            from cp in _dbContext.CompanyPermissions
            join p in _dbContext.Permissions on cp.PermissionId equals p.Id
            where userIds.Contains(cp.TaxUserId) && cp.IsGranted
            select new { cp.TaxUserId, p.Code };

        var userPermissions = await permissionsQuery.ToListAsync(ct);
        var permissionsByUser = userPermissions
            .GroupBy(x => x.TaxUserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToList());

        // Asignar roles y permisos a cada usuario
        foreach (var user in users)
        {
            if (rolesByUser.TryGetValue(user.Id, out var roles))
            {
                user.RoleNames = roles;
            }

            if (permissionsByUser.TryGetValue(user.Id, out var permissions))
            {
                user.CustomPermissions = permissions;
            }
        }
    }
}
