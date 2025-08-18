using Applications.DTOs.CompanyDTOs;
using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetAllCompaniesHandler
    : IRequestHandler<GetAllCompaniesQuery, ApiResponse<List<CompanyDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAllCompaniesHandler> _logger;

    public GetAllCompaniesHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAllCompaniesHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CompanyDTO>>> Handle(
        GetAllCompaniesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companiesQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                join tu in _dbContext.TaxUsers on c.Id equals tu.CompanyId
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _dbContext.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
                // JOINs para TaxUser address
                join ta in _dbContext.Addresses on tu.AddressId equals ta.Id into taxUserAddresses
                from ta in taxUserAddresses.DefaultIfEmpty()
                join tcountry in _dbContext.Countries
                    on ta.CountryId equals tcountry.Id
                    into taxUserCountries
                from tcountry in taxUserCountries.DefaultIfEmpty()
                join tstate in _dbContext.States on ta.StateId equals tstate.Id into taxUserStates
                from tstate in taxUserStates.DefaultIfEmpty()
                where tu.IsOwner == true
                select new CompanyDTO
                {
                    Id = c.Id,
                    IsCompany = c.IsCompany,
                    FullName = c.FullName,
                    CompanyName = c.CompanyName,
                    Brand = c.Brand,
                    Phone = c.Phone,
                    Description = c.Description,
                    Domain = c.Domain,
                    CreatedAt = c.CreatedAt,

                    // Información del TaxUser Owner
                    AdminUserId = tu.Id,
                    AdminEmail = tu.Email,
                    AdminName = tu.Name,
                    AdminLastName = tu.LastName,
                    AdminPhoneNumber = tu.PhoneNumber,
                    AdminPhotoUrl = tu.PhotoUrl,
                    AdminIsActive = tu.IsActive,
                    AdminConfirmed = tu.Confirm ?? false,
                    AdminAddress =
                        ta != null
                            ? new AddressDTO
                            {
                                CountryId = ta.CountryId,
                                StateId = ta.StateId,
                                City = ta.City,
                                Street = ta.Street,
                                Line = ta.Line,
                                ZipCode = ta.ZipCode,
                                CountryName = tcountry.Name,
                                StateName = tstate.Name,
                            }
                            : null,

                    // Información del CustomPlan
                    CustomPlanId = cp.Id,
                    CustomPlanPrice = cp.Price,
                    CustomPlanIsActive = cp.IsActive,
                    CustomPlanUserLimit = cp.UserLimit,
                    CustomPlanStartDate = cp.StartDate,
                    CustomPlanRenewDate = cp.RenewDate,
                    CustomPlanIsRenewed = cp.isRenewed,

                    // Conteo de TaxUsers
                    CurrentTaxUserCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),

                    // Dirección de la company
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

                    // Inicializar colecciones
                    BaseModules = new List<string>(),
                    AdditionalModules = new List<string>(),
                    AdminRoleNames = new List<string>(),
                };

            var companies = await companiesQuery.ToListAsync(cancellationToken);

            // Obtener roles de los Owners
            if (companies.Any())
            {
                var adminIds = companies.Select(c => c.AdminUserId).ToList();
                var rolesQuery =
                    from ur in _dbContext.UserRoles
                    join r in _dbContext.Roles on ur.RoleId equals r.Id
                    where adminIds.Contains(ur.TaxUserId)
                    select new { ur.TaxUserId, r.Name };

                var adminRoles = await rolesQuery.ToListAsync(cancellationToken);
                var rolesByAdmin = adminRoles
                    .GroupBy(x => x.TaxUserId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

                // Asignar roles a cada company
                foreach (var company in companies)
                {
                    if (rolesByAdmin.TryGetValue(company.AdminUserId, out var roles))
                    {
                        company.AdminRoleNames = roles;
                    }
                }
            }

            // Obtener módulos para cada company
            if (companies.Any())
            {
                await PopulateCompanyModulesAsync(companies, cancellationToken);
            }

            _logger.LogInformation("Retrieved {Count} companies successfully", companies.Count);
            return new ApiResponse<List<CompanyDTO>>(
                true,
                "Companies retrieved successfully",
                companies
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies: {Message}", ex.Message);
            return new ApiResponse<List<CompanyDTO>>(false, ex.Message, new List<CompanyDTO>());
        }
    }

    private async Task PopulateCompanyModulesAsync(List<CompanyDTO> companies, CancellationToken ct)
    {
        var companyIds = companies.Select(c => c.Id).ToList();

        var modulesQuery =
            from cm in _dbContext.CustomModules
            join cp in _dbContext.CustomPlans on cm.CustomPlanId equals cp.Id
            join m in _dbContext.Modules on cm.ModuleId equals m.Id
            where companyIds.Contains(cp.CompanyId) && cm.IsIncluded
            select new
            {
                CompanyId = cp.CompanyId,
                ModuleName = m.Name,
                HasService = m.ServiceId != null,
            };

        var modulesByCompany = await modulesQuery.ToListAsync(ct);
        var moduleGroups = modulesByCompany.GroupBy(m => m.CompanyId);

        foreach (var company in companies)
        {
            var companyModules = moduleGroups.FirstOrDefault(g => g.Key == company.Id);
            if (companyModules != null)
            {
                company.BaseModules = companyModules
                    .Where(m => m.HasService)
                    .Select(m => m.ModuleName)
                    .ToList();

                company.AdditionalModules = companyModules
                    .Where(m => !m.HasService)
                    .Select(m => m.ModuleName)
                    .ToList();
            }
        }
    }
}
