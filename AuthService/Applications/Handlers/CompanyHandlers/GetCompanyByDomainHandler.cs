using Applications.DTOs.CompanyDTOs;
using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetCompanyByDomainHandler
    : IRequestHandler<GetCompanyByDomainQuery, ApiResponse<CompanyDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyByDomainHandler> _logger;

    public GetCompanyByDomainHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyByDomainHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyDTO>> Handle(
        GetCompanyByDomainQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Misma lógica que GetCompanyById pero filtrado por Domain
            var companyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _dbContext.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
                where c.Domain == request.Domain
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

                    // Información del CustomPlan
                    CustomPlanId = cp.Id,
                    CustomPlanPrice = cp.Price,
                    CustomPlanIsActive = cp.IsActive,
                    CustomPlanStartDate = cp.StartDate,
                    CustomPlanEndDate = cp.EndDate,
                    CustomPlanRenewDate = cp.RenewDate,
                    CustomPlanIsRenewed = cp.isRenewed,

                    // Conteos correctos
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

                    // Inicializar colecciones
                    BaseModules = new List<string>(),
                    AdditionalModules = new List<string>(),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);

            if (company == null)
            {
                _logger.LogWarning("Company not found with domain: {Domain}", request.Domain);
                return new ApiResponse<CompanyDTO>(false, "Company not found", null!);
            }

            // Obtener módulos
            await PopulateCompanyModulesAsync(company, cancellationToken);

            _logger.LogInformation(
                "Company retrieved successfully by domain: {Domain}",
                request.Domain
            );
            return new ApiResponse<CompanyDTO>(true, "Company retrieved successfully", company);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company by domain {Domain}: {Message}",
                request.Domain,
                ex.Message
            );
            return new ApiResponse<CompanyDTO>(false, ex.Message, null!);
        }
    }

    /// <summary>
    /// Helper method para poblar módulos (mismo que GetCompanyById)
    /// </summary>
    private async Task PopulateCompanyModulesAsync(CompanyDTO company, CancellationToken ct)
    {
        var modulesQuery =
            from cm in _dbContext.CustomModules
            join cp in _dbContext.CustomPlans on cm.CustomPlanId equals cp.Id
            join m in _dbContext.Modules on cm.ModuleId equals m.Id
            where cp.CompanyId == company.Id && cm.IsIncluded
            select new { ModuleName = m.Name, HasService = m.ServiceId != null };

        var modules = await modulesQuery.ToListAsync(ct);

        company.BaseModules = modules.Where(m => m.HasService).Select(m => m.ModuleName).ToList();

        company.AdditionalModules = modules
            .Where(m => !m.HasService)
            .Select(m => m.ModuleName)
            .ToList();
    }
}
