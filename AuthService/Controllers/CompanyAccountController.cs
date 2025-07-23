using AuthService.Commands.CompanyUserCommands;
using AuthService.DTOs.UserDTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyAccountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CompanyAccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Confirma la cuenta de un usuario empresarial
        /// </summary>
        /// <param name="dto">Datos de confirmación con email y token</param>
        /// <returns>Resultado de la confirmación</returns>
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmCompanyAccount([FromBody] EmailConfirmDto dto)
        {
            var command = new CompanyAccountConfirmCommands(dto.Email, dto.Token);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
    }
}
