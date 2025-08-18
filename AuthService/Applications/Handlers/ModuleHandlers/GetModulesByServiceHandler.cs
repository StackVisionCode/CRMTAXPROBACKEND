using AuthService.DTOs.ModuleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ModuleQueries;

namespace AuthService.Handlers.ModuleHandlers;

public class GetModulesByServiceHandler
    : IRequestHandler<GetModulesByServiceQuery, ApiResponse<IEnumerable<ModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetModulesByServiceHandler> _logger;

    public GetModulesByServiceHandler(
        ApplicationDbContext dbContext,
        ILogger<GetModulesByServiceHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ModuleDTO>>> Handle(
        GetModulesByServiceQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el Service existe
            var serviceExists = await _dbContext.Services.AnyAsync(
                s => s.Id == request.ServiceId,
                cancellationToken
            );

            if (!serviceExists)
            {
                return new ApiResponse<IEnumerable<ModuleDTO>>(false, "Service not found", null!);
            }

            var modulesQuery =
                from m in _dbContext.Modules
                where m.ServiceId == request.ServiceId
                orderby m.Name
                select new
                {
                    Module = m,
                    ServiceName = (
                        from s in _dbContext.Services
                        where s.Id == m.ServiceId
                        select s.Name
                    ).FirstOrDefault(),
                };

            var modulesData = await modulesQuery.ToListAsync(cancellationToken);

            var modulesDtos = modulesData
                .Select(md => new ModuleDTO
                {
                    Id = md.Module.Id,
                    Name = md.Module.Name,
                    Description = md.Module.Description,
                    Url = md.Module.Url,
                    IsActive = md.Module.IsActive,
                    ServiceId = md.Module.ServiceId,
                    ServiceName = md.ServiceName,
                })
                .ToList();

            return new ApiResponse<IEnumerable<ModuleDTO>>(
                true,
                "Modules by service retrieved successfully",
                modulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting Modules by Service: {ServiceId}",
                request.ServiceId
            );
            return new ApiResponse<IEnumerable<ModuleDTO>>(
                false,
                "Error retrieving Modules by Service",
                null!
            );
        }
    }
}
