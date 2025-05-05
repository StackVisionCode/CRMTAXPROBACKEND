using AuthService.DTOs.PermissionDTOs;
using AutoMapper;
using Commands.PermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.PermissionHandlers;

public class GetAllPermissionHandler : IRequestHandler<GetAllPermissionQuery, ApiResponse<List<PermissionDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllPermissionHandler> _logger;

    public GetAllPermissionHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllPermissionHandler> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<PermissionDTO>>> Handle(GetAllPermissionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await _dbContext.Permissions.ToListAsync(cancellationToken);
            if (permissions == null || !permissions.Any())
            {
                return new ApiResponse<List<PermissionDTO>>(false, "No permissions found", null!);
            }

            var permissionDtos = _mapper.Map<List<PermissionDTO>>(permissions);
            _logger.LogInformation("Permissions retrieved successfully: {Permissions}", permissionDtos);
            return new ApiResponse<List<PermissionDTO>>(true, "Permissions retrieved successfully", permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions: {Message}", ex.Message);
            return new ApiResponse<List<PermissionDTO>>(false, ex.Message, null!);
        }
    }
}