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
            //  Query completa con CustomPlan y conteos correctos
            var companiesQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _dbContext.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
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

                    //  Información del CustomPlan
                    CustomPlanId = cp.Id,
                    CustomPlanPrice = cp.Price,
                    CustomPlanIsActive = cp.IsActive,
                    CustomPlanStartDate = cp.StartDate,
                    CustomPlanEndDate = cp.EndDate,
                    CustomPlanRenewDate = cp.RenewDate,
                    CustomPlanIsRenewed = cp.isRenewed,

                    //  Conteos separados por tipo de usuario
                    CurrentTaxUserCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    CurrentUserCompanyCount = _dbContext.UserCompanies.Count(uc =>
                        uc.CompanyId == c.Id
                    ),

                    // Dirección
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

                    //  Inicializar colecciones (se llenarán después)
                    BaseModules = new List<string>(),
                    AdditionalModules = new List<string>(),
                };

            var companies = await companiesQuery.ToListAsync(cancellationToken);

            //  Obtener módulos para cada company
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

    /// <summary>
    ///  Popula información de módulos para las companies
    /// </summary>
    private async Task PopulateCompanyModulesAsync(List<CompanyDTO> companies, CancellationToken ct)
    {
        var companyIds = companies.Select(c => c.Id).ToList();

        // Obtener todos los módulos de todas las companies
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

        // Agrupar por company y asignar
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
