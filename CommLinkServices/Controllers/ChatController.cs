using System.Security.Claims;
using CommLinkServices.Application.DTOs;
using CommLinkServices.Infrastructure.Commands;
using CommLinkServices.Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkServices.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // ⬅️ all endpoints require a valid JWT
public sealed class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _log;

    public ChatController(IMediator mediator, ILogger<ChatController> log)
    {
        _mediator = mediator;
        _log = log;
    }

    /// <summary>Helper: extracts the GUID stored in the JWT ("sub" or NameIdentifier).</summary>
    private Guid CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(raw, out var id))
            throw new InvalidOperationException("Invalid or missing subject claim");

        return id;
    }

    // ──────────────────────────────────────────────
    // 1️⃣  NEW – Ensure (or create) conversation
    // ──────────────────────────────────────────────
    /// <remarks>
    /// Returns <c>ApiResponse&lt;Guid&gt;</c> with the conversation Id.
    /// If it already exists, you still get the same Id.
    /// </remarks>
    [HttpPost("conversation/{otherUserId:guid}")]
    public async Task<IActionResult> EnsureConversation(Guid otherUserId, CancellationToken ct)
    {
        var cmd = new EnsureConversationCommand(CurrentUserId(), otherUserId);
        var result = await _mediator.Send(cmd, ct);

        return result.Success is true ? Ok(result) : BadRequest(result);
    }

    // ──────────────────────────────────────────────
    // 2️⃣  Chat – send a message
    // ──────────────────────────────────────────────
    [HttpPost("conversation/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequestDto body,
        CancellationToken ct
    )
    {
        var cmd = new SendMessageCommand(conversationId, CurrentUserId(), body);
        var result = await _mediator.Send(cmd, ct);

        return result.Success is true ? Ok(result) : BadRequest(result);
    }

    // ──────────────────────────────────────────────
    // 3️⃣  Chat – list messages
    // ──────────────────────────────────────────────
    [HttpGet("conversation/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        DateTime? after,
        CancellationToken ct
    )
    {
        var qry = new GetMessagesQuery(conversationId, CurrentUserId(), after);
        var result = await _mediator.Send(qry, ct);

        return result.Success is true ? Ok(result) : BadRequest(result);
    }

    // ──────────────────────────────────────────────
    // 4️⃣  Conversations list (sidebar)
    // ──────────────────────────────────────────────
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var qry = new GetConversationsQuery(CurrentUserId());
        var result = await _mediator.Send(qry, ct);

        return result.Success is true ? Ok(result) : BadRequest(result);
    }

    // ──────────────────────────────────────────────
    // 5️⃣  Start call
    // ──────────────────────────────────────────────
    [HttpPost("conversation/{conversationId:guid}/call")]
    public async Task<IActionResult> StartCall(
        Guid conversationId,
        [FromBody] StartCallRequestDto body,
        CancellationToken ct
    )
    {
        var cmd = new StartCallCommand(conversationId, CurrentUserId(), body);
        var result = await _mediator.Send(cmd, ct);

        return result.Success is true ? Ok(result) : BadRequest(result);
    }

    // ──────────────────────────────────────────────
    // 6️⃣  End call
    // ──────────────────────────────────────────────
    [HttpPost("call/end")]
    public async Task<IActionResult> EndCall(
        [FromBody] EndCallRequestDto body,
        CancellationToken ct
    )
    {
        var cmd = new EndCallCommand(CurrentUserId(), body);
        var result = await _mediator.Send(cmd, ct);

        return result.Success is true ? Ok(result) : BadRequest(result);
    }
}
