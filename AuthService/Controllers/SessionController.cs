using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.DTOs.PaginationDTO;
using AuthService.DTOs.SessionDTOs;
using Commands.SessionCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Queries.SessionQueries;

namespace AuthService.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class SessionController : ControllerBase
  {
    private readonly IMediator _mediator;
    public SessionController(IMediator mediator)
    {
      _mediator = mediator;
    }

    [HttpPost("Login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login([FromBody] LoginRequestDTO dto)
    {

      try
      {
        if (!ModelState.IsValid)
        {
          return BadRequest(new ApiResponse<LoginResponseDTO>(false, "Invalid input data"));
        }

        var command = new LoginCommands(
            dto,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers["User-Agent"].ToString());

        var result = await _mediator.Send(command);
        return Ok(result);
      }
      catch (Exception ex)
      {

        return StatusCode(500, new ApiResponse<LoginResponseDTO>(false, ex.Message));
      }
    }

    [Authorize]
    [HttpPost("Logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
      // --- sesión ----------------------------------------------------------
      var sidRaw = User.FindFirst("sid")?.Value ?? HttpContext.Items["SessionId"] as string;
      if (!Guid.TryParse(sidRaw, out var sessionId))
        return BadRequest(new ApiResponse<bool>(false, "Invalid session"));

      // --- usuario ---------------------------------------------------------
      var userId = GetUserId(User);
      if (userId is null)
        return BadRequest(new ApiResponse<bool>(false, "Invalid user identity"));

      var cmd = new LogoutCommand(sessionId, userId.Value);
      var result = await _mediator.Send(cmd);
      return Ok(result);
    }

    [Authorize]
    [HttpPost("LogoutAll")]
    public async Task<ActionResult<ApiResponse<bool>>> LogoutAll()
    {
      var userId = GetUserId(User);
      if (userId is null)
        return BadRequest(new ApiResponse<bool>(false, "Invalid user identity"));

      var cmd = new LogoutAllCommands(userId.Value);
      var result = await _mediator.Send(cmd);
      return Ok(result);
    }

    [Authorize]
    [HttpGet("Active")]
    public async Task<ActionResult<ApiResponse<List<SessionDTO>>>> GetActiveSessions()
    {
      var userId = GetUserId(User);
      if (userId is null)
        return BadRequest(new ApiResponse<List<SessionDTO>>(false, "Invalid user identity"));

      var query = new GetActiveSessionsQuery(userId.Value);
      var result = await _mediator.Send(query);
      return Ok(result);
    }

    [Authorize]
    [HttpGet("GetSessionById")]
    public async Task<ActionResult<ApiResponse<SessionDTO>>> GetSessionById(Guid id)
    {
      var query = new GetSessionByIdQuery(id);
      var result = await _mediator.Send(query);
      return Ok(result);
    }

    [Authorize]
    [HttpGet("GetAllSessions")]
    public async Task<ActionResult<ApiResponse<PaginatedResultDTO<SessionDTO>>>> GetAllSessions()
    {
      var command = new GetAllSessionsQuery();
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }

    [HttpGet("IsValid")]
    public async Task<IActionResult> IsValid([FromQuery] string sid)
    {
      if (string.IsNullOrWhiteSpace(sid) || !Guid.TryParse(sid, out Guid SessionId))
      {
        return BadRequest(new { Success = false, Message = "Invalid session id format." });
      }

      bool isActive = await _mediator.Send(new ValidateSessionQuery(SessionId));
      return isActive ? Ok(new { Success = true, Message = "Session is valid" })
                    : Unauthorized(new { Success = false, Message = "Session is invalid or expired" });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
      var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      return Guid.TryParse(raw, out var g) ? g : null;
    }
  }
}