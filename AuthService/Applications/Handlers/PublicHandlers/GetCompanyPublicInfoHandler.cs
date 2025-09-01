using AuthService.Applications.Common;
using Common;
using DTOs.PublicDTOs;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.PublicQueries;

namespace Handlers.PublicHandlers;

public class GetCompanyPublicInfoHandler
    : IRequestHandler<GetCompanyPublicInfoQuery, ApiResponse<CompanyPublicInfoDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyPublicInfoHandler> _logger;

    public GetCompanyPublicInfoHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyPublicInfoHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyPublicInfoDTO>> Handle(
        GetCompanyPublicInfoQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query con información limitada para seguridad (sin CustomPlans)
            var companyQuery =
                from c in _dbContext.Companies
                join owner in _dbContext.TaxUsers on c.Id equals owner.CompanyId
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join s in _dbContext.States on a.StateId equals s.Id into states
                from s in states.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                where c.Id == request.CompanyId && owner.IsOwner == true && owner.IsActive == true
                select new CompanyPublicInfoDTO
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    Brand = c.Brand,
                    Domain = c.Domain,
                    Phone = c.Phone,
                    City = a != null ? a.City : null,
                    State = s != null ? s.Name : null,
                    CountryName = country != null ? country.Name : null,

                    // Información del owner
                    OwnerId = owner.Id,
                    OwnerName = owner.Name,
                    OwnerLastName = owner.LastName,
                    OwnerPhotoUrl = owner.PhotoUrl,
                    OwnerIsActive = owner.IsActive,

                    // Información del plan basada en ServiceLevel
                    ServiceLevel = c.ServiceLevel,
                    HasActivePlan = true, // Si existe en AuthService y tiene owner activo, está operativa
                    PlanType = DeterminePlanTypeByServiceLevel(c.ServiceLevel),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);

            if (company == null)
            {
                _logger.LogWarning(
                    "Public info requested for non-existent Company: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<CompanyPublicInfoDTO>(
                    false,
                    "Company not found or inactive",
                    null!
                );
            }

            _logger.LogInformation(
                "Public Company info retrieved: {CompanyId} - {CompanyName} (ServiceLevel: {ServiceLevel})",
                request.CompanyId,
                company.CompanyName,
                company.ServiceLevel
            );

            return new ApiResponse<CompanyPublicInfoDTO>(
                true,
                "Company info retrieved successfully",
                company
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving public Company info for {CompanyId}: {Message}",
                request.CompanyId,
                ex.Message
            );
            return new ApiResponse<CompanyPublicInfoDTO>(false, "Internal server error", null!);
        }
    }

    private static string DeterminePlanTypeByServiceLevel(ServiceLevel serviceLevel)
    {
        return serviceLevel switch
        {
            ServiceLevel.Basic => "Basic",
            ServiceLevel.Standard => "Standard",
            ServiceLevel.Pro => "Professional",
            ServiceLevel.Developer => "Enterprise", // No exponer "Developer" públicamente
            _ => "Basic",
        };
    }
}
