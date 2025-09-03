
using Common;
using LandingService.Applications.DTO;
using LandingService.Infrastructure.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LandingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterController : ControllerBase
{
    private readonly IMediator _mediator;
    public RegisterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("CreateRegister")]
    public async Task<ActionResult<ApiResponse<bool>>> CreateRegister([FromBody] RegisterDTO requestDto)
    {
        if (requestDto == null)
        {
            return BadRequest(new { message = "Invalid request data" });
        }

        var command = new CreateRegisterCommands(requestDto);

        var result = await _mediator.Send(command);
        if (result.Success == false)
            return BadRequest(new { message = "Failed to create a register" });
        return Ok(result);
    }
    
    
    [HttpPost("Login")]
    public async Task<ActionResult<ApiResponse<bool>>> Login([FromBody] LoginDTO loginDTO)
    {
        if (loginDTO == null)
        {
            return BadRequest(new { message = "Invalid request data" });
        }
        
        var command = new LoginCommands(loginDTO);

         var result = await _mediator.Send(command);
        if (result.Success==false)
            return BadRequest(new { message = "Failed to access" });
        return Ok(result);
    }
}


