using Applications.DTOs.AddressDTOs;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserTaxHandlers;

public class GetAllUserTaxHandler : IRequestHandler<GetAllUserQuery, ApiResponse<List<UserGetDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAllUserTaxHandler> _logger;

    public GetAllUserTaxHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAllUserTaxHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<List<UserGetDTO>>> Handle(
        GetAllUserQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query con JOINs potentes - sin CustomPlans
            var userQuery =
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
                orderby u.IsOwner descending, u.CreatedAt descending // Owners primero
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

                    // Company info - disponible en AuthService
                    CompanyFullName = c.FullName,
                    CompanyName = c.CompanyName,
                    CompanyBrand = c.Brand,
                    CompanyIsIndividual = !c.IsCompany,
                    CompanyDomain = c.Domain,
                    CompanyServiceLevel = c.ServiceLevel, // NUEVO

                    // Dirección de la company
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

                    RoleNames = new List<string>(),
                    CustomPermissions = new List<string>(),
                };

            var users = await userQuery.ToListAsync(cancellationToken);

            if (!users.Any())
            {
                _logger.LogInformation("No tax users found");
                return new ApiResponse<List<UserGetDTO>>(
                    true,
                    "No tax users found",
                    new List<UserGetDTO>()
                );
            }

            // Obtener roles y permisos en consultas separadas
            await PopulateUsersRolesAndPermissionsAsync(users, cancellationToken);

            _logger.LogInformation("Retrieved {Count} tax users successfully", users.Count);
            return new ApiResponse<List<UserGetDTO>>(
                true,
                "Tax users retrieved successfully",
                users
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax users: {Message}", ex.Message);
            return new ApiResponse<List<UserGetDTO>>(false, ex.Message, new List<UserGetDTO>());
        }
    }

    private async Task PopulateUsersRolesAndPermissionsAsync(
        List<UserGetDTO> users,
        CancellationToken cancellationToken
    )
    {
        var userIds = users.Select(u => u.Id).ToList();

        // Obtener roles por separado
        var rolesQuery =
            from ur in _dbContext.UserRoles
            join r in _dbContext.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.TaxUserId)
            select new { ur.TaxUserId, r.Name };

        var userRoles = await rolesQuery.ToListAsync(cancellationToken);
        var rolesByUser = userRoles
            .GroupBy(x => x.TaxUserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        // Obtener permisos personalizados
        var customPermissionsQuery =
            from cp in _dbContext.CompanyPermissions
            join p in _dbContext.Permissions on cp.PermissionId equals p.Id
            where userIds.Contains(cp.TaxUserId) && cp.IsGranted
            select new { cp.TaxUserId, p.Code };

        var customPermissions = await customPermissionsQuery.ToListAsync(cancellationToken);
        var permissionsByUser = customPermissions
            .GroupBy(x => x.TaxUserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToList());

        // Asignar roles y permisos a usuarios
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
