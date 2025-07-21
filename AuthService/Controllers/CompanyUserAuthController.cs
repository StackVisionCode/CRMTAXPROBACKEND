using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.DTOs.CompanyUserSessionDTOs;
using AuthService.DTOs.SessionDTOs;
using Commands.SessionCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CompanyUserQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth/company-user")]
public class CompanyUserAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanyUserAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login(
        [FromBody] CompanyUserLoginRequestDTO dto
    )
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<LoginResponseDTO>(false, "Invalid input data"));

            var command = new CompanyUserLoginCommand(
                dto,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers["User-Agent"].ToString()
            );

            var result = await _mediator.Send(command);

            if (!(result?.Success ?? false))
                return Unauthorized(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LoginResponseDTO>(false, ex.Message));
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        var sidRaw = User.FindFirst("sid")?.Value ?? HttpContext.Items["SessionId"] as string;
        if (!Guid.TryParse(sidRaw, out var sessionId))
            return BadRequest(new ApiResponse<bool>(false, "Invalid session"));

        var userId = GetUserId(User);
        if (userId is null)
            return BadRequest(new ApiResponse<bool>(false, "Invalid user identity"));

        var cmd = new CompanyUserLogoutCommand(sessionId, userId.Value);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("logout-all")]
    public async Task<ActionResult<ApiResponse<bool>>> LogoutAll()
    {
        var userId = GetUserId(User);
        if (userId is null)
            return BadRequest(new ApiResponse<bool>(false, "Invalid user identity"));

        var cmd = new CompanyUserLogoutAllCommand(userId.Value);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<ReadCompanyUserSessionDTO>>>> GetSessions()
    {
        var userId = GetUserId(User);
        if (userId is null)
            return BadRequest(
                new ApiResponse<List<ReadCompanyUserSessionDTO>>(false, "Invalid user identity")
            );

        var query = new GetCompanyUserSessionsQuery(userId.Value);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("sessions/active")]
    public async Task<ActionResult<ApiResponse<List<CompanyUserSessionDTO>>>> GetActiveSessions()
    {
        var userId = GetUserId(User);
        if (userId is null)
            return BadRequest(
                new ApiResponse<List<CompanyUserSessionDTO>>(false, "Invalid user identity")
            );

        var query = new GetActiveCompanyUserSessionsQuery(userId.Value);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var raw =
            user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var g) ? g : null;
    }
}
