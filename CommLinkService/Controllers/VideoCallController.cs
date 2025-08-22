using CommLinkService.Application.Commands;
using CommLinkService.Application.Common.Shared;
using CommLinkService.Application.Queries;
using Common;
using DTOs.RequestControllerDTOs;
using DTOs.RoomDTOs;
using DTOs.VideoCallDTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VideoCallController : ControllerBase
{
    private readonly IMediator _mediator;

    public VideoCallController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<VideoCallDTO>>> StartVideoCall(
        [FromBody] StartVideoCallRequest request
    )
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var command = new StartVideoCallCommand(
            request.RoomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            userInfo.CompanyId
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("end")]
    public async Task<ActionResult<ApiResponse<VideoCallEndDTO>>> EndVideoCall(
        [FromBody] EndVideoCallRequest request
    )
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var command = new EndVideoCallCommand(
            request.RoomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            request.CallId
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<ActiveVideoCallDTO>>>> GetActiveCalls()
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var query = new GetActiveVideoCallsQuery(
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            userInfo.CompanyId
        );

        var result = await _mediator.Send(query);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("participant/status")]
    public async Task<ActionResult<ApiResponse<RoomParticipantDTO>>> UpdateParticipantStatus(
        [FromBody] UpdateStatusRequest request
    )
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var command = new UpdateParticipantStatusCommand(
            request.RoomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            request.IsMuted,
            request.IsVideoEnabled
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
