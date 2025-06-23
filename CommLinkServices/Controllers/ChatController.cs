using System.Security.Claims;
using CommLinkServices.Application.DTOs;
using CommLinkServices.Infrastructure.Commands;
using CommLinkServices.Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ChatController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid UserId() =>
            Guid.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier /* ó "sub" */
                )!.Value
            );

        [HttpPost("conversation/{conversationId}/messages")]
        public async Task<IActionResult> Send(
            Guid conversationId,
            Guid UserId,
            [FromBody] SendMessageRequestDto body
        )
        {
            var command = new SendMessageCommand(conversationId, UserId, body);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpPost("conversation/{conversationId}/call")]
        public async Task<IActionResult> StartCall(
            Guid conversationId,
            Guid UserId,
            [FromBody] StartCallRequestDto body
        )
        {
            var command = new StartCallCommand(conversationId, UserId, body);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpPost("call/end")]
        public async Task<IActionResult> EndCall([FromBody] EndCallRequestDto body, Guid UserId)
        {
            var result = await _mediator.Send(new EndCallCommand(UserId, body));

            return result.Success == true ? Ok(result) : BadRequest(result);
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> Conversations(Guid UserId)
        {
            var command = new GetConversationsQuery(UserId);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("conversation/{conversationId}/messages")]
        public async Task<IActionResult> Messages(
            Guid conversationId,
            Guid UserId,
            DateTime? after = null
        )
        {
            var command = new GetMessagesQuery(conversationId, UserId, after);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
