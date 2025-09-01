using Common;
using DTOs.ModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ModuleQueries;

namespace AuthService.Handlers.ModuleHandlers;

public class GetModuleByIdHandler : IRequestHandler<GetModuleByIdQuery, ApiResponse<ModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetModuleByIdHandler> _logger;

    public GetModuleByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetModuleByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ModuleDTO>> Handle(
        GetModuleByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var moduleQuery =
                from m in _dbContext.Modules
                where m.Id == request.ModuleId
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

            var moduleData = await moduleQuery.FirstOrDefaultAsync(cancellationToken);
            if (moduleData?.Module == null)
            {
                _logger.LogWarning("Module not found: {ModuleId}", request.ModuleId);
                return new ApiResponse<ModuleDTO>(false, "Module not found", null!);
            }

            var moduleDto = new ModuleDTO
            {
                Id = moduleData.Module.Id,
                Name = moduleData.Module.Name,
                Description = moduleData.Module.Description,
                Url = moduleData.Module.Url,
                IsActive = moduleData.Module.IsActive,
                ServiceId = moduleData.Module.ServiceId,
                ServiceName = moduleData.ServiceName,
            };

            return new ApiResponse<ModuleDTO>(true, "Module retrieved successfully", moduleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Module: {ModuleId}", request.ModuleId);
            return new ApiResponse<ModuleDTO>(false, "Error retrieving Module", null!);
        }
    }
}
