using System.Security.Claims;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RoomController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RoomController> _logger;

    public RoomController(IMediator mediator, ILogger<RoomController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new CreateRoomCommand(
            request.Name,
            request.Type,
            userId,
            request.ParticipantIds,
            request.MaxParticipants ?? 10
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("my-rooms")]
    public async Task<IActionResult> GetMyRooms()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var query = new GetUserRoomsQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{roomId}/messages")]
    public async Task<IActionResult> GetRoomMessages(
        Guid roomId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50
    )
    {
        var query = new GetRoomMessagesQuery(roomId, pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{roomId}/participants")]
    public async Task<IActionResult> GetRoomParticipants(Guid roomId)
    {
        var query = new GetRoomParticipantsQuery(roomId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("direct-between")]
    public async Task<IActionResult> GetRoomBetween(
        [FromQuery] string user1,
        [FromQuery] string user2,
        [FromQuery] RoomType? type /* opcional */
    )
    {
        var caller = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var u1 = Guid.Parse(user1);
        var u2 = Guid.Parse(user2);

        if (caller != u1 && caller != u2)
            return Forbid();

        var dto = await _mediator.Send(new GetRoomBetweenUsersQuery(u1, u2, type));

        // Mantener compatibilidad con front viejo
        return Ok(dto); // dto puede ser null
    }

    [HttpPost("{roomId}/join")]
    public async Task<IActionResult> JoinRoom(Guid roomId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var connectionId = Guid.NewGuid().ToString(); // En producción, obtener del WebSocket

        var command = new JoinRoomCommand(roomId, userId, connectionId);
        var result = await _mediator.Send(command);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpPost("{roomId}/leave")]
    public async Task<IActionResult> LeaveRoom(Guid roomId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new LeaveRoomCommand(roomId, userId);
        var result = await _mediator.Send(command);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpPost("{roomId}/typing")]
    public async Task<IActionResult> SetTypingStatus(
        Guid roomId,
        [FromBody] TypingStatusRequest request
    )
    {
        // Este endpoint es principalmente para fallback
        // Normalmente se maneja via WebSocket
        return Ok(new { message = "Use WebSocket for real-time typing indicators" });
    }
}

public sealed record CreateRoomRequest(
    string Name,
    RoomType Type,
    List<Guid>? ParticipantIds,
    int? MaxParticipants
);

public sealed record TypingStatusRequest(bool IsTyping);
