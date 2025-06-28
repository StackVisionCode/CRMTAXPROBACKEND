using AuthService.Commands.UserConfirmCommands;
using AuthService.DTOs.UserDTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmAccount([FromBody] EmailConfirmDto dto)
        {
            var command = new AccountConfirmCommands(dto.Email, dto.Token);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
