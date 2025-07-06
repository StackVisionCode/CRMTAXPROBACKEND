using AuthService.DTOs.RoleDTOs;
using Commands.RoleCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.RoleQueries;
using SharedLibrary.Authorizations;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HasPermission("Role.Create")]
    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] RoleDTO roleDto)
    {
        var command = new CreateRoleCommands(roleDto);
        var result = await _mediator.Send(command);
        if (result == null)
            return BadRequest(new { message = "Failed to create a role" });
        return Ok(result);
    }

    [HasPermission("Role.Update")]
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] RoleDTO roleDto)
    {
        var command = new UpdateRoleCommands(roleDto);
        var result = await _mediator.Send(command);
        if (result == null)
            return BadRequest(new { message = "Failed to update a role" });
        return Ok(result);
    }

    [HasPermission("Role.Delete")]
    [HttpDelete("Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var command = new DeleteRoleCommands(id);
        var result = await _mediator.Send(command);
        if (result == null)
            return BadRequest(new { message = "Failed to delete a role" });
        return Ok(result);
    }

    [HasPermission("Role.Read")]
    [HttpGet("GetAll")]
    public async Task<ActionResult> GetAll()
    {
        var command = new GetAllRoleQuery();
        var result = await _mediator.Send(command);
        if (result.Success == false)
            return BadRequest(new { result });

        return Ok(result);
    }

    [HasPermission("Role.Read")]
    [HttpGet("GetById")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var command = new GetRoleByIdQuery(id);
        var result = await _mediator.Send(command);
        if (result.Success == false)
            return BadRequest(new { result });

        return Ok(result);
    }
}
