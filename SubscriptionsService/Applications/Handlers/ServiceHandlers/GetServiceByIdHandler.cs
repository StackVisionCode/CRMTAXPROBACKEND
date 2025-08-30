using Common;
using DTOs.ServiceDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ServiceQueries;

namespace AuthService.Handlers.ServiceHandlers;

public class GetServiceByIdHandler : IRequestHandler<GetServiceByIdQuery, ApiResponse<ServiceDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetServiceByIdHandler> _logger;

    public GetServiceByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetServiceByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ServiceDTO>> Handle(
        GetServiceByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var serviceQuery =
                from s in _dbContext.Services
                where s.Id == request.ServiceId
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

            var serviceData = await serviceQuery.FirstOrDefaultAsync(cancellationToken);
            if (serviceData?.Service == null)
            {
                _logger.LogWarning("Service not found: {ServiceId}", request.ServiceId);
                return new ApiResponse<ServiceDTO>(false, "Service not found", null!);
            }

            var serviceDto = new ServiceDTO
            {
                Id = serviceData.Service.Id,
                Name = serviceData.Service.Name,
                Title = serviceData.Service.Title,
                Description = serviceData.Service.Description,
                Features = serviceData.Service.Features,
                Price = serviceData.Service.Price,
                UserLimit = serviceData.Service.UserLimit,
                IsActive = serviceData.Service.IsActive,
                ModuleNames = serviceData.ModuleNames,
                ModuleIds = serviceData.ModuleIds,
                CreatedAt = serviceData.Service.CreatedAt,
            };

            return new ApiResponse<ServiceDTO>(true, "Service retrieved successfully", serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Service: {ServiceId}", request.ServiceId);
            return new ApiResponse<ServiceDTO>(false, "Error retrieving Service", null!);
        }
    }
}
