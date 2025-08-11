using Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.UserCompanyDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetMyCompanyUsersHandler
    : IRequestHandler<GetMyCompanyUsersQuery, ApiResponse<List<UserCompanyDTO>>>
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

    public async Task<ApiResponse<List<UserCompanyDTO>>> Handle(
        GetMyCompanyUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Verificar que la company preparadora existe
            var companyInfoQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.CompanyId
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    // Obtener información del servicio base para límites
                    ServiceInfo = (
                        from s in _dbContext.Services
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id && m.ServiceId == s.Id && cm.IsIncluded
                        select s
                    ).FirstOrDefault(),
                };

            var companyInfo = await companyInfoQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyInfo?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyId);
                return new ApiResponse<List<UserCompanyDTO>>(
                    false,
                    "Company not found",
                    new List<UserCompanyDTO>()
                );
            }

            // 2. Obtener UserCompanies (empleados) de la company
            var employeesQuery =
                from uc in _dbContext.UserCompanies
                join c in _dbContext.Companies on uc.CompanyId equals c.Id
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                join ua in _dbContext.Addresses on uc.AddressId equals ua.Id into userAddresses
                from ua in userAddresses.DefaultIfEmpty()
                join ucountry in _dbContext.Countries
                    on ua.CountryId equals ucountry.Id
                    into userCountries
                from ucountry in userCountries.DefaultIfEmpty()
                join ustate in _dbContext.States on ua.StateId equals ustate.Id into userStates
                from ustate in userStates.DefaultIfEmpty()
                join ca in _dbContext.Addresses on c.AddressId equals ca.Id into companyAddresses
                from ca in companyAddresses.DefaultIfEmpty()
                join ccountry in _dbContext.Countries
                    on ca.CountryId equals ccountry.Id
                    into companyCountries
                from ccountry in companyCountries.DefaultIfEmpty()
                join cstate in _dbContext.States on ca.StateId equals cstate.Id into companyStates
                from cstate in companyStates.DefaultIfEmpty()
                where uc.CompanyId == request.CompanyId
                orderby uc.CreatedAt descending
                select new UserCompanyDTO
                {
                    Id = uc.Id,
                    CompanyId = uc.CompanyId,
                    Email = uc.Email,
                    Name = uc.Name,
                    LastName = uc.LastName,
                    PhoneNumber = uc.PhoneNumber,
                    PhotoUrl = uc.PhotoUrl,
                    IsActive = uc.IsActive,
                    IsActiveDate = uc.IsActiveDate,
                    Confirm = uc.Confirm ?? false,
                    CreatedAt = uc.CreatedAt,

                    // Dirección del empleado
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

                    // Información de la company preparadora (su empleador)
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

                    RoleNames = new List<string>(),
                    CustomPermissions = new List<string>(),
                };

            var employees = await employeesQuery.ToListAsync(cancellationToken);

            if (!employees.Any())
            {
                _logger.LogInformation(
                    "No employees found for company: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<List<UserCompanyDTO>>(
                    true,
                    "No employees found for this company",
                    new List<UserCompanyDTO>()
                );
            }

            // 3. Obtener roles y permisos personalizados de los empleados
            await PopulateUserCompanyRolesAndPermissionsAsync(employees, cancellationToken);

            // 4. Información adicional del plan para context
            var planInfo =
                companyInfo.ServiceInfo != null
                    ? $"Plan allows up to {companyInfo.ServiceInfo.UserLimit} users"
                    : "Custom plan";

            _logger.LogInformation(
                "Retrieved {Count} employees (UserCompanies) for company {CompanyId}. {PlanInfo}",
                employees.Count,
                request.CompanyId,
                planInfo
            );

            return new ApiResponse<List<UserCompanyDTO>>(
                true,
                $"Company employees retrieved successfully. {planInfo}",
                employees
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving employees for company {CompanyId}: {Message}",
                request.CompanyId,
                ex.Message
            );
            return new ApiResponse<List<UserCompanyDTO>>(
                false,
                "Error retrieving company employees",
                new List<UserCompanyDTO>()
            );
        }
    }

    /// <summary>
    /// Popula roles y permisos personalizados para los empleados (UserCompanies)
    /// </summary>
    private async Task PopulateUserCompanyRolesAndPermissionsAsync(
        List<UserCompanyDTO> employees,
        CancellationToken ct
    )
    {
        var employeeIds = employees.Select(uc => uc.Id).ToList();

        // Obtener roles de los empleados
        var rolesQuery =
            from ucr in _dbContext.UserCompanyRoles
            join r in _dbContext.Roles on ucr.RoleId equals r.Id
            where employeeIds.Contains(ucr.UserCompanyId)
            select new { ucr.UserCompanyId, r.Name };

        var employeeRoles = await rolesQuery.ToListAsync(ct);
        var rolesByEmployee = employeeRoles
            .GroupBy(x => x.UserCompanyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        // Obtener permisos personalizados de los empleados
        var permissionsQuery =
            from cp in _dbContext.CompanyPermissions
            where employeeIds.Contains(cp.UserCompanyId) && cp.IsGranted
            select new { cp.UserCompanyId, cp.Code };

        var employeePermissions = await permissionsQuery.ToListAsync(ct);
        var permissionsByEmployee = employeePermissions
            .GroupBy(x => x.UserCompanyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToList());

        // Asignar roles y permisos a cada empleado
        foreach (var employee in employees)
        {
            if (rolesByEmployee.TryGetValue(employee.Id, out var roles))
            {
                employee.RoleNames = roles;
            }

            if (permissionsByEmployee.TryGetValue(employee.Id, out var permissions))
            {
                employee.CustomPermissions = permissions;
            }
        }
    }
}
