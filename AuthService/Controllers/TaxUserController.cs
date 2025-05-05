
using System.Security.Claims;
using AuthService.DTOs.UserDTOs;
using Commands.UserCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Queries.UserQueries;

namespace AuthService.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class TaxUserController : ControllerBase
  {
    private readonly IMediator _mediator;
    public TaxUserController(IMediator mediator)
    {
      _mediator = mediator;
    }

    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] NewUserDTO userDto)
    {
      // Mapeas el DTO al Command (usando AutoMapper)
      var command = new CreateTaxUserCommands(userDto);
      var result = await _mediator.Send(command);
      if (result==null) return BadRequest(new { message = "Failed to create user" });      
      return Ok(result);
    }

    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] UpdateUserDTO userDto)
    {
      var command = new UpdateTaxUserCommands(userDto);
      var result = await _mediator.Send(command);
      if (result==null) return BadRequest(new { message = "Failed to update user" });
      return Ok(result);
    }

    [HttpDelete("Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
      var command = new DeleteTaxUserCommands(id);
      var result = await _mediator.Send(command);
      if (result==null) return BadRequest(new { message = "Failed to delete user" });
      return Ok(result);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<UserGetDTO[]>>> GetAll()
    {
      var result = await _mediator.Send(new GetAllUserQuery());

      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);      
    }

    [HttpGet("GetByUserId")]
    public async Task<IActionResult> GetById(int id)
    {
      var command = new GetTaxUserByIdQuery(id);
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }

    [Authorize]
    [HttpGet("Profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDTO>>> GetProfile()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized(new ApiResponse<UserProfileDTO>(false, "Invalid session"));

        var command = new GetTaxUserByIdQuery(userId);
        var result = await _mediator.Send(command);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }
  }
}