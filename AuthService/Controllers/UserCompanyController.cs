using AuthService.Commands.InvitationCommands;
using AuthService.DTOs.UserCompanyDTOs;
using Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserCompanyController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserCompanyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Envía una invitación a un email para unirse como UserCompany
    /// </summary>
    [HttpPost("invite")]
    [Authorize] // Solo usuarios autenticados pueden enviar invitaciones
    public async Task<ActionResult<ApiResponse<bool>>> SendInvitation(
        [FromBody] SendInvitationDTO invitation,
        [FromHeader(Name = "Origin")] string origin = ""
    )
    {
        var command = new SendUserCompanyInvitationCommand(invitation, origin);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Registra un UserCompany usando un token de invitación
    /// </summary>
    [HttpPost("register-by-invitation")]
    public async Task<ActionResult<ApiResponse<bool>>> RegisterByInvitation(
        [FromBody] RegisterByInvitationDTO registration,
        [FromHeader(Name = "Origin")] string origin = ""
    )
    {
        var command = new RegisterUserCompanyByInvitationCommand(registration, origin);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Valida un token de invitación y retorna información de la company
    /// </summary>
    [HttpGet("validate-invitation/{token}")]
    public async Task<ActionResult<ApiResponse<InvitationValidationDTO>>> ValidateInvitation(
        string token
    )
    {
        var command = new ValidateInvitationCommand(token);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
