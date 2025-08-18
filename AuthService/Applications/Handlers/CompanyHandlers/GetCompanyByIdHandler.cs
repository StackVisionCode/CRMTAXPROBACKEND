using Applications.DTOs.CompanyDTOs;
using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetCompanyByIdHandler : IRequestHandler<GetCompanyByIdQuery, ApiResponse<CompanyDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyByIdHandler> _logger;

    public GetCompanyByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyDTO>> Handle(
        GetCompanyByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyQuery =
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
                where c.Id == request.Id && tu.IsOwner == true
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
                    CustomPlanUserLimit = cp.UserLimit,
                    CustomPlanIsActive = cp.IsActive,
                    CustomPlanStartDate = cp.StartDate,
                    CustomPlanRenewDate = cp.RenewDate,
                    CustomPlanIsRenewed = cp.isRenewed,

                    // Conteo de TaxUsers
                    CurrentTaxUserCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),

                    // Dirección de la Company
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

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);

            if (company == null)
            {
                return new ApiResponse<CompanyDTO>(false, "Company not found", null!);
            }

            // Obtener roles del Owner
            var rolesQuery =
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == company.AdminUserId
                select r.Name;

            var roles = await rolesQuery.ToListAsync(cancellationToken);
            company.AdminRoleNames = roles;

            // Obtener módulos de la company
            await PopulateCompanyModulesAsync(company, cancellationToken);

            return new ApiResponse<CompanyDTO>(true, "Company retrieved successfully", company);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company {Id}: {Message}",
                request.Id,
                ex.Message
            );
            return new ApiResponse<CompanyDTO>(false, ex.Message, null!);
        }
    }

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
