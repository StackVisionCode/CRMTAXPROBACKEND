using System.Security.Claims;
using AuthService.DTOs.PaginationDTO;
using AuthService.DTOs.SessionDTOs;
using AuthService.DTOs.UserDTOs;
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
    public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login([FromBody] UserLoginDTO dto)
    {

    try
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<LoginResponseDTO>(false, "Invalid input data"));
        }

        var command = new LoginCommands(
            dto.Email,
            dto.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers["User-Agent"].ToString(),
            dto.RememberMe);

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
      // Obtener sesión actual del contexto (desde el token)
      var sessionId = HttpContext.Items["SessionId"] as int? ?? 0;
      if (sessionId == 0)
      {
        return BadRequest(new ApiResponse<bool>(false, "Session not found"));
      }

      // Obtener ID del usuario del token
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
      {
        return BadRequest(new ApiResponse<bool>(false, "Invalid user identity"));
      }

      var command = new LogoutCommand(sessionId, userId);
      var result = await _mediator.Send(command);
      return Ok(result);
    }

    [Authorize]
    [HttpPost("LogoutAll")]
    public async Task<ActionResult<ApiResponse<bool>>> LogoutAll()
    {
      // Obtener ID del usuario del token
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
      {
        return BadRequest(new ApiResponse<bool>(false, "Invalid user identity"));
      }

      var command = new LogoutAllCommands(userId);
      var result = await _mediator.Send(command);
      return Ok(result);
    }

    [Authorize]
    [HttpGet("Active")]
    public async Task<ActionResult<ApiResponse<List<SessionDTO>>>> GetActiveSessions()
    {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
      {
        return BadRequest(new ApiResponse<List<SessionDTO>>(false, "Invalid user identity"));
      }

      var query = new GetActiveSessionsQuery(userId);
      var result = await _mediator.Send(query);
      return Ok(result);
    }

    [Authorize]
    [HttpGet("GetSessionById")]
    public async Task<ActionResult<ApiResponse<SessionDTO>>> GetSessionById(int id)
    {
      var query = new GetSessionByIdQuery(id);
      var result = await _mediator.Send(query);
      return Ok(result);
    }

    [Authorize]
    [HttpGet("GetAllSessions")]
    public async Task<ActionResult<ApiResponse<PaginatedResultDTO<SessionDTO>>>> GetAllSessions([FromQuery] int? pageSize = null, [FromQuery] int? pageNumber = null)
    {
      var query = new GetAllSessionsQuery(pageSize, pageNumber);
      var result = await _mediator.Send(query);
      return Ok(result);
    }
  }
}