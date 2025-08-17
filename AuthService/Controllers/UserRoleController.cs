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

    /// <summary>
    /// Asignar un rol a un usuario
    /// </summary>
    [HasPermission("RolePermission.Create")]
    [HttpPost("assign")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignRole(
        [FromBody] AssignRoleRequest request
    )
    {
        var command = new AssignRoleToUserCommand(request.UserId, request.RoleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Asignar rol via query parameters (mantener compatibilidad)
    /// </summary>
    [HasPermission("RolePermission.Create")]
    [HttpPost("assign/{userId:guid}/{roleId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignRoleByParams(Guid userId, Guid roleId)
    {
        var command = new AssignRoleToUserCommand(userId, roleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Remover un rol de un usuario
    /// </summary>
    [HasPermission("RolePermission.Delete")]
    [HttpDelete("remove")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveRole(
        [FromBody] RemoveRoleRequest request
    )
    {
        var command = new RemoveRoleFromUserCommand(request.UserId, request.RoleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Remover rol via query parameters (mantener compatibilidad)
    /// </summary>
    [HasPermission("RolePermission.Delete")]
    [HttpDelete("remove/{userId:guid}/{roleId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveRoleByParams(Guid userId, Guid roleId)
    {
        var command = new RemoveRoleFromUserCommand(userId, roleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Actualizar todos los roles de un usuario de una vez
    /// </summary>
    [HasPermission("RolePermission.Update")]
    [HttpPut("update-roles")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUserRoles(
        [FromBody] UpdateUserRolesRequest request
    )
    {
        var command = new UpdateUserRolesCommand(request.UserId, request.RoleIds);
        var result = await _med.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los roles de un usuario
    /// </summary>
    [HasPermission("Role.Read")]
    [HttpGet("{userId:guid}/roles")]
    public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetRolesByUser(Guid userId)
    {
        var query = new GetRolesByUserIdQuery(userId);
        var result = await _med.Send(query);
        return Ok(result);
    }
}
