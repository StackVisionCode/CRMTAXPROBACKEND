using Applications.DTOs.AddressDTOs;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetUsersByCompanyIdHandler
    : IRequestHandler<GetUsersByCompanyIdQuery, ApiResponse<List<UserGetDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetUsersByCompanyIdHandler> _logger;

    public GetUsersByCompanyIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetUsersByCompanyIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<List<UserGetDTO>>> Handle(
        GetUsersByCompanyIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que la company existe
            var companyExists = await _dbContext.Companies.AnyAsync(
                c => c.Id == request.CompanyId,
                cancellationToken
            );

            if (!companyExists)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyId);
                return new ApiResponse<List<UserGetDTO>>(
                    false,
                    "Company not found",
                    new List<UserGetDTO>()
                );
            }

            // Query simplificado - sin CustomPlans
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
                orderby u.IsOwner descending, u.CreatedAt descending
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

                    // Información de la company (disponible en AuthService)
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

            var users = await usersQuery.ToListAsync(cancellationToken);

            if (users.Any())
            {
                // Obtener roles y permisos personalizados
                await PopulateUserRolesAndPermissionsAsync(users, cancellationToken);
            }

            _logger.LogInformation(
                "Retrieved {Count} TaxUsers for company {CompanyId} (Owner: {OwnerCount}, Regular: {RegularCount})",
                users.Count,
                request.CompanyId,
                users.Count(u => u.IsOwner),
                users.Count(u => !u.IsOwner)
            );

            return new ApiResponse<List<UserGetDTO>>(
                true,
                "Company users retrieved successfully",
                users
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
            return new ApiResponse<List<UserGetDTO>>(false, ex.Message, new List<UserGetDTO>());
        }
    }

    private async Task PopulateUserRolesAndPermissionsAsync(
        List<UserGetDTO> users,
        CancellationToken cancellationToken
    )
    {
        var userIds = users.Select(u => u.Id).ToList();

        // Obtener roles de los TaxUsers
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
        var permissionsQuery =
            from cp in _dbContext.CompanyPermissions
            join p in _dbContext.Permissions on cp.PermissionId equals p.Id
            where userIds.Contains(cp.TaxUserId) && cp.IsGranted
            select new { cp.TaxUserId, p.Code };

        var userPermissions = await permissionsQuery.ToListAsync(cancellationToken);
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
