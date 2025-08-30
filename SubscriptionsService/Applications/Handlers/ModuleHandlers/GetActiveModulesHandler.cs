using Common;
using DTOs.ModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ModuleQueries;

namespace AuthService.Handlers.ModuleHandlers;

public class GetActiveModulesHandler
    : IRequestHandler<GetActiveModulesQuery, ApiResponse<IEnumerable<ModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetActiveModulesHandler> _logger;

    public GetActiveModulesHandler(
        ApplicationDbContext dbContext,
        ILogger<GetActiveModulesHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ModuleDTO>>> Handle(
        GetActiveModulesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var modulesQuery =
                from m in _dbContext.Modules
                where m.IsActive && (request.ServiceId == null || m.ServiceId == request.ServiceId)
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
                "Active modules retrieved successfully",
                modulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active Modules");
            return new ApiResponse<IEnumerable<ModuleDTO>>(
                false,
                "Error retrieving active Modules",
                null!
            );
        }
    }
}
