using AuthService.DTOs.ServiceDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ServiceQueries;

namespace AuthService.Handlers.ServiceHandlers;

public class GetAllServicesHandler
    : IRequestHandler<GetAllServicesQuery, ApiResponse<IEnumerable<ServiceDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAllServicesHandler> _logger;

    public GetAllServicesHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAllServicesHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ServiceDTO>>> Handle(
        GetAllServicesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var servicesQuery =
                from s in _dbContext.Services
                where request.IsActive == null || s.IsActive == request.IsActive
                orderby s.Name
                select new
                {
                    Service = s,
                    ModuleNames = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id && m.IsActive
                        select m.Name
                    ).ToList(),
                    ModuleIds = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id && m.IsActive
                        select m.Id
                    ).ToList(),
                };

            var servicesData = await servicesQuery.ToListAsync(cancellationToken);

            var servicesDtos = servicesData
                .Select(sd => new ServiceDTO
                {
                    Id = sd.Service.Id,
                    Name = sd.Service.Name,
                    Title = sd.Service.Title,
                    Description = sd.Service.Description,
                    Features = sd.Service.Features,
                    Price = sd.Service.Price,
                    UserLimit = sd.Service.UserLimit,
                    IsActive = sd.Service.IsActive,
                    ModuleNames = sd.ModuleNames,
                    ModuleIds = sd.ModuleIds,
                    CreatedAt = sd.Service.CreatedAt,
                })
                .ToList();

            return new ApiResponse<IEnumerable<ServiceDTO>>(
                true,
                "Services retrieved successfully",
                servicesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all Services");
            return new ApiResponse<IEnumerable<ServiceDTO>>(
                false,
                "Error retrieving Services",
                null!
            );
        }
    }
}
