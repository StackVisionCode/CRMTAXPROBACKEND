using AuthService.Domains.Roles;
using Commands.CustomerRoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hanlders.CustomerRoleHandlers;

public class AssignRoleToCustomerHandler
    : IRequestHandler<AssignRoleToCustomerCommand, ApiResponse<bool>>
{
    private readonly ILogger<AssignRoleToCustomerHandler> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AssignRoleToCustomerHandler(
        ILogger<AssignRoleToCustomerHandler> logger,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<bool>> Handle(
        AssignRoleToCustomerCommand request,
        CancellationToken cancellationToken
    )
    {
        // 1.  Comprobamos que el rol existe
        if (!await _dbContext.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken))
            return new ApiResponse<bool>(false, "Role not found", false);

        // 2.  Evitemos duplicados
        var exists = await _dbContext.CustomerRoles.AnyAsync(
            cr => cr.CustomerId == request.CustomerId && cr.RoleId == request.RoleId,
            cancellationToken
        );
        if (exists)
            return new ApiResponse<bool>(false, "Customer already has this role", false);

        // 3.  Asignamos el rol al cliente
        await _dbContext.CustomerRoles.AddAsync(
            new CustomerRole
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                RoleId = request.RoleId,
                CreatedAt = DateTime.UtcNow,
            },
            cancellationToken
        );

        var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return new ApiResponse<bool>(
            result,
            result ? "Role assigned successfully" : "Failed to assign role",
            result
        );
    }
}
