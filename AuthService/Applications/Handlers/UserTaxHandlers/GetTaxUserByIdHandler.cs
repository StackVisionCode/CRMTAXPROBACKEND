using Applications.DTOs.AddressDTOs;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserTaxHandlers;

public class GetTaxUserByIdHandler : IRequestHandler<GetTaxUserByIdQuery, ApiResponse<UserGetDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetTaxUserByIdHandler> _logger;

    public GetTaxUserByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetTaxUserByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<UserGetDTO>> Handle(
        GetTaxUserByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query optimizado sin CustomPlans (que no se usaba)
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
                where u.Id == request.Id
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

                    // Company info completa (con ServiceLevel agregado)
                    CompanyFullName = c.FullName,
                    CompanyName = c.CompanyName,
                    CompanyBrand = c.Brand,
                    CompanyIsIndividual = !c.IsCompany,
                    CompanyDomain = c.Domain,
                    CompanyServiceLevel = c.ServiceLevel, // AGREGADO

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

            var user = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Tax user not found: {UserId}", request.Id);
                return new ApiResponse<UserGetDTO>(false, "Tax user not found", null!);
            }

            // Obtener roles del usuario
            var rolesQuery =
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == request.Id
                select r.Name;

            var roles = await rolesQuery.ToListAsync(cancellationToken);
            user.RoleNames = roles;

            // Obtener permisos personalizados (CompanyPermissions)
            var customPermissionsQuery =
                from cp in _dbContext.CompanyPermissions
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where cp.TaxUserId == request.Id && cp.IsGranted
                select p.Code;

            var customPermissions = await customPermissionsQuery.ToListAsync(cancellationToken);
            user.CustomPermissions = customPermissions;

            _logger.LogInformation(
                "Tax user retrieved successfully: UserId={UserId}, IsOwner={IsOwner}, ServiceLevel={ServiceLevel}, "
                    + "RoleCount={RoleCount}, CustomPermissionCount={PermissionCount}",
                request.Id,
                user.IsOwner,
                user.CompanyServiceLevel,
                roles.Count,
                customPermissions.Count
            );

            return new ApiResponse<UserGetDTO>(true, "Tax user retrieved successfully", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tax user {Id}: {Message}", request.Id, ex.Message);
            return new ApiResponse<UserGetDTO>(false, "Error retrieving tax user", null!);
        }
    }
}
