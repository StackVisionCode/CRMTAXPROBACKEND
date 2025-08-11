using Applications.DTOs.CompanyDTOs;
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

            // Obtener TaxUsers (preparadores) de la company
            var preparersQuery =
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
                where u.CompanyId == request.CompanyId
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

                    // Información de la company preparadora
                    CompanyFullName = c.FullName,
                    CompanyName = c.CompanyName,
                    CompanyBrand = c.Brand,
                    CompanyIsIndividual = !c.IsCompany,
                    CompanyDomain = c.Domain,

                    // Dirección de la company preparadora
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

            var preparers = await preparersQuery.ToListAsync(cancellationToken);

            if (preparers.Any())
            {
                // Obtener roles de los preparadores
                var userIds = preparers.Select(u => u.Id).ToList();
                var rolesQuery =
                    from ur in _dbContext.UserRoles
                    join r in _dbContext.Roles on ur.RoleId equals r.Id
                    where userIds.Contains(ur.TaxUserId)
                    select new { ur.TaxUserId, r.Name };

                var userRoles = await rolesQuery.ToListAsync(cancellationToken);
                var rolesByUser = userRoles
                    .GroupBy(x => x.TaxUserId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

                // Asignar roles a preparadores
                foreach (var preparer in preparers)
                {
                    if (rolesByUser.TryGetValue(preparer.Id, out var roles))
                    {
                        preparer.RoleNames = roles;
                    }
                }
            }

            _logger.LogInformation(
                "Retrieved {Count} preparers (TaxUsers) for company {CompanyId}",
                preparers.Count,
                request.CompanyId
            );
            return new ApiResponse<List<UserGetDTO>>(
                true,
                "Company preparers retrieved successfully",
                preparers
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving preparers for company {CompanyId}: {Message}",
                request.CompanyId,
                ex.Message
            );
            return new ApiResponse<List<UserGetDTO>>(false, ex.Message, new List<UserGetDTO>());
        }
    }
}
