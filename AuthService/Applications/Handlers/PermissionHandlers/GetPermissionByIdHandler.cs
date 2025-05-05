using AuthService.DTOs.PermissionDTOs;
using AutoMapper;
using Commands.PermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.PermissionHandlers;

public class GetPermissionByIdHandler : IRequestHandler<GetPermissionByIdQuery, ApiResponse<PermissionDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPermissionByIdHandler> _logger;

    public GetPermissionByIdHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetPermissionByIdHandler> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PermissionDTO>> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await _dbContext.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.PermissionId, cancellationToken);

            if (permission is null)
                return new(false, "Permission not found");

            var permissionDto = _mapper.Map<PermissionDTO>(permission);

            return new ApiResponse<PermissionDTO>(true, "Ok", permissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permission {Id}", request.PermissionId);
            return new(false, ex.Message);
        }
    }
}