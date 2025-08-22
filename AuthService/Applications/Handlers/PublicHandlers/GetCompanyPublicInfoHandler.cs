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
            // Query con información muy limitada para seguridad
            var companyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                join owner in _dbContext.TaxUsers on c.Id equals owner.CompanyId
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join s in _dbContext.States on a.StateId equals s.Id into states
                from s in states.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                where
                    c.Id == request.CompanyId
                    && owner.IsOwner == true
                    && owner.IsActive == true
                    && cp.IsActive == true // Solo compañías activas
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
                    OwnerId = owner.Id,
                    OwnerName = owner.Name,
                    OwnerLastName = owner.LastName,
                    OwnerPhotoUrl = owner.PhotoUrl,
                    OwnerIsActive = owner.IsActive,
                    HasActivePlan = cp.IsActive,
                    PlanType = DeterminePlanTypeByUserLimit(cp.UserLimit), // Método helper
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);

            if (company == null)
            {
                _logger.LogWarning(
                    "Public info requested for non-existent or inactive Company: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<CompanyPublicInfoDTO>(
                    false,
                    "Company not found or inactive",
                    null!
                );
            }

            _logger.LogInformation(
                "Public Company info retrieved: {CompanyId} - {CompanyName}",
                request.CompanyId,
                company.CompanyName
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

    private static string DeterminePlanTypeByUserLimit(int userLimit)
    {
        return userLimit switch
        {
            1 => "Basic",
            <= 4 => "Standard",
            <= 5 => "Pro",
            _ => "Enterprise",
        };
    }
}
