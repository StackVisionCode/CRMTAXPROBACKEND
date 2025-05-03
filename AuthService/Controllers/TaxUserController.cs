
using AuthService.DTOs.UserDTOs;
using Commands.UserCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.UserQueries;
using UserDTOS;

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










    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<UserDTO[]>>> GetAll()
    {
      var result = await _mediator.Send(new GetAllUserQuery());

      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);

      
    }

  //   [HttpGet("GetByUserId")]
  //   public async Task<IActionResult> GetById(int id)
  //   {
  //     ApiResponse<UserDTO> result = await _userRead.GetUserById(id);
  //     if (result.Success == false) return BadRequest(new { result });

  //     return Ok(result);
  //   }

  //   [HttpGet("GetProfile")]
  //   public async Task<ActionResult<ApiResponse<UserDTO>>> GetProfile()
  //   {
  //     var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

  //     if (!int.TryParse(idClaim, out var userId))
  //       return Unauthorized(new { message = "Invalid session" });

  //     var result = await _userRead.GetProfile(userId);

  //     return result.Success ? Ok(result) : BadRequest(result);
  //   }
  }
}