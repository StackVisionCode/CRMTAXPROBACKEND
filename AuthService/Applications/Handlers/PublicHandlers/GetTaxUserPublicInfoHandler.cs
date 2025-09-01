using AuthService.Applications.Common;
using Common;
using DTOs.PublicDTOs;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.PublicQueries;

namespace Handlers.PublicHandlers;

public class GetTaxUserPublicInfoHandler
    : IRequestHandler<GetTaxUserPublicInfoQuery, ApiResponse<TaxUserPublicInfoDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetTaxUserPublicInfoHandler> _logger;

    public GetTaxUserPublicInfoHandler(
        ApplicationDbContext dbContext,
        ILogger<GetTaxUserPublicInfoHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<TaxUserPublicInfoDTO>> Handle(
        GetTaxUserPublicInfoQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query optimizado con información limitada para seguridad (sin CustomPlans)
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join s in _dbContext.States on a.StateId equals s.Id into states
                from s in states.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                where
                    u.Id == request.TaxUserId
                    && u.IsActive == true // Solo usuarios activos
                    && c.Id != Guid.Empty // Validación adicional
                select new TaxUserPublicInfoDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhotoUrl = u.PhotoUrl,
                    IsActive = u.IsActive,
                    IsOwner = u.IsOwner,

                    // Información de la company
                    CompanyId = u.CompanyId,
                    CompanyName = c.CompanyName,
                    CompanyBrand = c.Brand,
                    CompanyDomain = c.Domain,
                    CompanyPhone = c.Phone,
                    CompanyCity = a != null ? a.City : null,
                    CompanyState = s != null ? s.Name : null,
                    CompanyServiceLevel = c.ServiceLevel,

                    BasicRoles = new List<string>(),
                };

            var user = await userQuery.FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                _logger.LogWarning(
                    "Public info requested for non-existent or inactive TaxUser: {TaxUserId}",
                    request.TaxUserId
                );
                return new ApiResponse<TaxUserPublicInfoDTO>(
                    false,
                    "User not found or inactive",
                    null!
                );
            }

            // Obtener solo roles básicos (sin permisos sensibles)
            var basicRolesQuery =
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == request.TaxUserId
                select r.Name;

            var roles = await basicRolesQuery.ToListAsync(cancellationToken);

            // Filtrar roles sensibles para seguridad pública
            user.BasicRoles = FilterSensitiveRoles(roles);

            _logger.LogInformation(
                "Public TaxUser info retrieved: {TaxUserId} from Company: {CompanyId} (ServiceLevel: {ServiceLevel})",
                request.TaxUserId,
                user.CompanyId,
                user.CompanyServiceLevel
            );

            return new ApiResponse<TaxUserPublicInfoDTO>(
                true,
                "User info retrieved successfully",
                user
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving public TaxUser info for {TaxUserId}: {Message}",
                request.TaxUserId,
                ex.Message
            );
            return new ApiResponse<TaxUserPublicInfoDTO>(false, "Internal server error", null!);
        }
    }

    private static List<string> FilterSensitiveRoles(List<string> roles)
    {
        // Solo mostrar roles públicamente apropiados
        return roles
            .Where(role =>
                !role.Contains("Developer")
                && !role.Contains("Administrator")
                && (role.Contains("User") || role.Contains("Customer"))
            )
            .ToList();
    }
}
