using AuthService.DTOs.ModuleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ModuleQueries;

namespace AuthService.Handlers.ModuleHandlers;

public class GetAllModulesHandler
    : IRequestHandler<GetAllModulesQuery, ApiResponse<IEnumerable<ModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAllModulesHandler> _logger;

    public GetAllModulesHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAllModulesHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ModuleDTO>>> Handle(
        GetAllModulesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var modulesQuery =
                from m in _dbContext.Modules
                where
                    (request.IsActive == null || m.IsActive == request.IsActive)
                    && (request.ServiceId == null || m.ServiceId == request.ServiceId)
                orderby m.Name
                select new
                {
                    Module = m,
                    ServiceName = m.ServiceId != null
                        ? (
                            from s in _dbContext.Services
                            where s.Id == m.ServiceId
                            select s.Name
                        ).FirstOrDefault()
                        : null,
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
                "Modules retrieved successfully",
                modulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all Modules");
            return new ApiResponse<IEnumerable<ModuleDTO>>(
                false,
                "Error retrieving Modules",
                null!
            );
        }
    }
}
