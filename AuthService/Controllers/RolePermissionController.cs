using AuthService.DTOs.RoleDTOs;
using Commands.RolePermissionCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class RolePermissionController : ControllerBase
  {
    private readonly IMediator _mediator;
    public RolePermissionController(IMediator mediator)
    {
      _mediator = mediator;
    }

    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] RolePermissionDTO rolePermissionDto)
    {
      var command = new CreateRolePermissionCommands(rolePermissionDto);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to create a role permission" });
      return Ok(result);
    }

    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] RolePermissionDTO rolePermissionDto)
    {
      var command = new UpdateRolePermissionCommands(rolePermissionDto);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to update a role permission" });
      return Ok(result);
    }

    [HttpDelete("Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
      var command = new DeleteRolePermissionCommands(id);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to delete a role permission" });
      return Ok(result);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult> GetAll()
    {
      var command = new GetAllRolePermissionQuery();
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }

    [HttpGet("GetById")]
    public async Task<IActionResult> GetById(int id)
    {
      var command = new GetRolePermissionByIdQuery(id);
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }
  }
}