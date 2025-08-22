using CommLinkService.Application.Commands;
using CommLinkService.Application.Common.Shared;
using Common;
using DTOs.MessageDTOs;
using DTOs.RequestControllerDTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMediator _mediator;

    public MessageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send")]
    public async Task<ActionResult<ApiResponse<MessageDTO>>> SendMessage(
        [FromBody] SendMessageDTO request
    )
    {
        var command = new SendMessageCommand(request);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{messageId}/edit")]
    public async Task<ActionResult<ApiResponse<MessageDTO>>> EditMessage(
        Guid messageId,
        [FromBody] EditMessageRequest request
    )
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var command = new EditMessageCommand(
            messageId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            request.Content
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{messageId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteMessage(Guid messageId)
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var command = new DeleteMessageCommand(
            messageId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{messageId}/react")]
    public async Task<ActionResult<ApiResponse<MessageReactionDTO>>> ReactToMessage(
        Guid messageId,
        [FromBody] ReactRequest request
    )
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var command = new ReactToMessageCommand(
            messageId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            userInfo.CompanyId,
            request.Emoji
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
