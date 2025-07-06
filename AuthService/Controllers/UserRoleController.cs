using AuthService.DTOs.RoleDTOs;
using Commands.UserRoleCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.UserRoleQueries;
using SharedLibrary.Authorizations;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserRoleController : ControllerBase
{
    private readonly IMediator _med;

    public UserRoleController(IMediator med)
    {
        _med = med;
    }

    [HasPermission("RolePermission.Create")]
    [HttpPost("Assign")]
    public async Task<ActionResult<ApiResponse<bool>>> Assign(Guid userId, Guid roleId)
    {
        var command = new AssignRoleToUserCommand(userId, roleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    [HasPermission("RolePermission.Delete")]
    [HttpDelete("Remove")]
    public async Task<ActionResult<ApiResponse<bool>>> Remove(Guid userId, Guid roleId)
    {
        var command = new RemoveRoleFromUserCommand(userId, roleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    [HasPermission("Role.Read")]
    [HttpGet("{userId:guid}/roles")]
    public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetRolesByUser(Guid userId)
    {
        var command = new GetRolesByUserIdQuery(userId);
        var result = await _med.Send(command);
        return Ok(result);
    }
}
