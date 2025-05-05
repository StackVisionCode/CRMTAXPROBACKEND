using AuthService.DTOs.PermissionDTOs;
using Commands.PermissionCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class PermissionController : ControllerBase
  {
    private readonly IMediator _mediator;

    public PermissionController(IMediator mediator)
    {
      _mediator = mediator;
    }

    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] PermissionDTO permissionDto)
    {
      var command = new CreatePermissionCommands(permissionDto);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to create a permission" });
      return Ok(result);
    }

    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] PermissionDTO permissionDto)
    {
      var command = new UpdatePermissionCommands(permissionDto);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to update a permission" });
      return Ok(result);
    }

    [HttpDelete("Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
      var command = new DeletePermissionCommands(id);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to delete a permission" });
      return Ok(result);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult> GetAll()
    {
      var command = new GetAllPermissionQuery();
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }

    [HttpGet("GetById")]
    public async Task<IActionResult> GetById(int id)
    {
      var command = new GetPermissionByIdQuery(id);
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }
  }
}