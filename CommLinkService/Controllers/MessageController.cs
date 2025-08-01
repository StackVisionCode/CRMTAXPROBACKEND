using System.Security.Claims;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MessageController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IMediator mediator, ILogger<MessageController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new SendMessageCommand(
            request.RoomId,
            userId,
            request.Content,
            request.Type,
            request.Metadata
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("{messageId}/edit")]
    public async Task<IActionResult> EditMessage(
        Guid messageId,
        [FromBody] EditMessageRequest request
    )
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new EditMessageCommand(messageId, userId, request.Content);
        var result = await _mediator.Send(command);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new DeleteMessageCommand(messageId, userId);
        var result = await _mediator.Send(command);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpPost("{messageId}/react")]
    public async Task<IActionResult> ReactToMessage(Guid messageId, [FromBody] ReactRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var command = new ReactToMessageCommand(messageId, userId, request.Emoji);
        var result = await _mediator.Send(command);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }
}

public sealed record SendMessageRequest(
    Guid RoomId,
    string Content,
    MessageType Type,
    string? Metadata
);

public sealed record EditMessageRequest(string Content);

public sealed record ReactRequest(string Emoji);
