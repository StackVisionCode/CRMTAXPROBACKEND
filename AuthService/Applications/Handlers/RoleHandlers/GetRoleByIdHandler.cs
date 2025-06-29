using AuthService.DTOs.RoleDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.RoleQueries;

namespace Handlers.RoleHandlers;

public class GetRoleByIdHandler : IRequestHandler<GetRoleByIdQuery, ApiResponse<RoleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRoleByIdHandler> _logger;

    public GetRoleByIdHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetRoleByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<RoleDTO>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var role = await (
                from r in _dbContext.Roles
                where r.Id == request.RoleId
                join rp in _dbContext.RolePermissions on r.Id equals rp.RoleId into rps
                from rp in rps.DefaultIfEmpty()
                join p in _dbContext.Permissions on rp.PermissionId equals p.Id into ps
                from p in ps.DefaultIfEmpty()

                group p by new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.PortalAccess,
                } into g
                select new RoleDTO
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Description = g.Key.Description,
                    PortalAccess = g.Key.PortalAccess,
                    PermissionCodes = g.Where(p => p != null)
                        .Select(p => p!.Code)
                        .Distinct()
                        .ToList(),
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (role is null)
                return new ApiResponse<RoleDTO>(false, "Role not found");

            var roleDto = _mapper.Map<RoleDTO>(role);

            return new ApiResponse<RoleDTO>(true, "Ok", roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching role {Id}", request.RoleId);
            return new(false, ex.Message);
        }
    }
}
