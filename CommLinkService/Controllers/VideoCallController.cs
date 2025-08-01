using System.Security.Claims;
using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class VideoCallController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VideoCallController> _logger;

    public VideoCallController(IMediator mediator, ILogger<VideoCallController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartVideoCall([FromBody] StartVideoCallRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new StartVideoCallCommand(
            request.RoomId,
            userId,
            request.ParticipantIds ?? new List<Guid>()
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("end")]
    public async Task<IActionResult> EndVideoCall([FromBody] EndVideoCallRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new EndVideoCallCommand(request.RoomId, userId, request.CallId);

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCalls()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var query = new GetActiveCallsQuery(userId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    [HttpPut("participant/status")]
    public async Task<IActionResult> UpdateParticipantStatus([FromBody] UpdateStatusRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new UpdateParticipantStatusCommand(
            request.RoomId,
            userId,
            request.IsMuted,
            request.IsVideoEnabled
        );

        var result = await _mediator.Send(command);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }
}

public sealed record StartVideoCallRequest(Guid RoomId, List<Guid>? ParticipantIds);

public sealed record EndVideoCallRequest(Guid RoomId, Guid CallId);

public sealed record UpdateStatusRequest(Guid RoomId, bool? IsMuted, bool? IsVideoEnabled);
