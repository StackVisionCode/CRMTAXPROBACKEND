using Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetMyCompanyUsersHandler
    : IRequestHandler<GetMyCompanyUsersQuery, ApiResponse<List<UserGetDTO>>>
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

    public async Task<ApiResponse<List<UserGetDTO>>> Handle(
        GetMyCompanyUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Primero verificar que la compañía existe
            var companyExistsQuery =
                from c in _dbContext.Companies
                where c.Id == request.CompanyId
                select c.Id;

            var companyExists = await companyExistsQuery.AnyAsync(cancellationToken);
            if (!companyExists)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyId);
                return new ApiResponse<List<UserGetDTO>>(
                    false,
                    "Company not found",
                    new List<UserGetDTO>()
                );
            }

            // 2. Query principal: obtener usuarios de MI compañía con rol "User" (excluyendo Administrator)
            var usersQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                // Join con dirección del usuario
                join ua in _dbContext.Addresses on u.AddressId equals ua.Id into userAddresses
                from ua in userAddresses.DefaultIfEmpty()
                join ucountry in _dbContext.Countries
                    on ua.CountryId equals ucountry.Id
                    into userCountries
                from ucountry in userCountries.DefaultIfEmpty()
                join ustate in _dbContext.States on ua.StateId equals ustate.Id into userStates
                from ustate in userStates.DefaultIfEmpty()
                // Join con dirección de la compañía
                join ca in _dbContext.Addresses on c.AddressId equals ca.Id into companyAddresses
                from ca in companyAddresses.DefaultIfEmpty()
                join ccountry in _dbContext.Countries
                    on ca.CountryId equals ccountry.Id
                    into companyCountries
                from ccountry in companyCountries.DefaultIfEmpty()
                join cstate in _dbContext.States on ca.StateId equals cstate.Id into companyStates
                from cstate in companyStates.DefaultIfEmpty()
                // Join con roles para filtrar solo usuarios "User" (no Administrator)
                join ur in _dbContext.UserRoles on u.Id equals ur.TaxUserId
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where u.CompanyId == request.CompanyId && r.Name == "User" // Solo usuarios con rol "User"
                select new UserGetDTO
                {
                    Id = u.Id,
                    CompanyId = u.CompanyId,
                    Email = u.Email,
                    Name = u.Name,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    PhotoUrl = u.PhotoUrl,
                    IsActive = u.IsActive,
                    Confirm = u.Confirm ?? false,
                    CreatedAt = u.CreatedAt,

                    // Dirección del usuario
                    Address =
                        ua != null
                            ? new AddressDTO
                            {
                                CountryId = ua.CountryId,
                                StateId = ua.StateId,
                                City = ua.City,
                                Street = ua.Street,
                                Line = ua.Line,
                                ZipCode = ua.ZipCode,
                                CountryName = ucountry.Name,
                                StateName = ustate.Name,
                            }
                            : null,

                    // Información de la compañía
                    CompanyFullName = c.FullName,
                    CompanyName = c.CompanyName,
                    CompanyBrand = c.Brand,
                    CompanyIsIndividual = !c.IsCompany,
                    CompanyDomain = c.Domain,

                    // Dirección de la compañía
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

                    RoleNames = new List<string>(), // Se llenará después
                };

            var users = await usersQuery.Distinct().ToListAsync(cancellationToken);

            if (!users.Any())
            {
                _logger.LogInformation(
                    "No users found for company: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<List<UserGetDTO>>(
                    true,
                    "No users found for this company",
                    new List<UserGetDTO>()
                );
            }

            // 3. Obtener TODOS los roles de cada usuario (no solo "User")
            var userIds = users.Select(u => u.Id).ToList();
            var rolesQuery =
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where userIds.Contains(ur.TaxUserId)
                select new { ur.TaxUserId, r.Name };

            var userRoles = await rolesQuery.ToListAsync(cancellationToken);
            var rolesByUser = userRoles
                .GroupBy(x => x.TaxUserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

            // 4. Asignar roles a cada usuario
            foreach (var user in users)
            {
                if (rolesByUser.TryGetValue(user.Id, out var roles))
                {
                    user.RoleNames = roles;
                }
            }

            _logger.LogInformation(
                "Retrieved {Count} users for company {CompanyId}",
                users.Count,
                request.CompanyId
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
            return new ApiResponse<List<UserGetDTO>>(
                false,
                "Error retrieving company users",
                new List<UserGetDTO>()
            );
        }
    }
}
