using Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.UserDTOs;
using AutoMapper;
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
            // Query mejorado con CustomPlan info
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
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

                    // Dirección del preparador
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

                    // Company info completa
                    CompanyFullName = c.FullName,
                    CompanyName = c.CompanyName,
                    CompanyBrand = c.Brand,
                    CompanyIsIndividual = !c.IsCompany,
                    CompanyDomain = c.Domain,

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

                    RoleNames = new List<string>(), // Se llenará después
                };

            var users = await userQuery.ToListAsync(cancellationToken);

            if (!users.Any())
            {
                return new ApiResponse<List<UserGetDTO>>(
                    true,
                    "No tax preparers found",
                    new List<UserGetDTO>()
                );
            }

            // Obtener roles por separado para evitar problemas de JOIN múltiple
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

            // Asignar roles a usuarios
            foreach (var user in users)
            {
                if (rolesByUser.TryGetValue(user.Id, out var roles))
                {
                    user.RoleNames = roles;
                }
            }

            _logger.LogInformation("Retrieved {Count} tax preparers successfully", users.Count);
            return new ApiResponse<List<UserGetDTO>>(
                true,
                "Tax preparers retrieved successfully",
                users
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax preparers: {Message}", ex.Message);
            return new ApiResponse<List<UserGetDTO>>(false, ex.Message, new List<UserGetDTO>());
        }
    }
}
