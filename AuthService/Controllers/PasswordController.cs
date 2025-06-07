using AuthService.Applications.DTOs;
using AuthService.Commands.ResetPasswordCommands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PasswordController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestPassword(
            [FromBody] EmailDto dto,
            [FromHeader(Name = "Origin")] string origin
        )
        {
            var command = new RequestPasswordResetCommands(dto.Email, origin);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("otp/send")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            var command = new SendOtpCommands(dto.Email, dto.Token);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("otp/validate")]
        public async Task<IActionResult> ValidateOtp([FromBody] OtpDto dto)
        {
            var command = new ValidateOtpCommands(dto.Email, dto.Otp);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var command = new ResetPasswordCommands(dto.Email, dto.NewPassword, dto.Token);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
