using CommLinkService.Application.Commands;
using CommLinkService.Application.Common.Shared;
using CommLinkService.Application.Queries;
using Common;
using DTOs.RequestControllerDTOs;
using DTOs.RoomDTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoomController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<RoomDTO>>> CreateRoom(
        [FromBody] CreateRoomDTO request
    )
    {
        var command = new CreateRoomCommand(request);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("my-rooms")]
    public async Task<ActionResult<ApiResponse<List<RoomDTO>>>> GetMyRooms()
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        if (userInfo.UserType == ParticipantType.TaxUser)
        {
            var query = new GetTaxUserRoomsQuery(
                userInfo.TaxUserId!.Value,
                userInfo.CompanyId!.Value
            );
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }
        else
        {
            var query = new GetCustomerRoomsQuery(userInfo.CustomerId!.Value);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }
    }

    [HttpGet("{roomId}/messages")]
    public async Task<ActionResult<ApiResponse<GetRoomMessagesResult>>> GetRoomMessages(
        Guid roomId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50
    )
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var query = new GetRoomMessagesQuery(
            roomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            pageNumber,
            pageSize
        );

        var result = await _mediator.Send(query);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{roomId}/participants")]
    public async Task<ActionResult<ApiResponse<List<RoomParticipantDTO>>>> GetRoomParticipants(
        Guid roomId
    )
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var query = new GetRoomParticipantsQuery(
            roomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId
        );

        var result = await _mediator.Send(query);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("direct-between")]
    public async Task<ActionResult<ApiResponse<RoomDTO?>>> GetRoomBetween(
        [FromQuery] Guid customerId,
        [FromQuery] Guid taxUserId,
        [FromQuery] Guid companyId,
        [FromQuery] RoomType? type = null
    )
    {
        var query = new GetRoomBetweenCustomerAndTaxUserQuery(
            customerId,
            taxUserId,
            companyId,
            type
        );
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{roomId}/join")]
    public async Task<ActionResult<ApiResponse<RoomParticipantDTO>>> JoinRoom(Guid roomId)
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var connectionId = Guid.NewGuid().ToString();

        var command = new JoinRoomCommand(
            roomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId,
            userInfo.CompanyId,
            connectionId
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{roomId}/leave")]
    public async Task<ActionResult<ApiResponse<bool>>> LeaveRoom(Guid roomId)
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var command = new LeaveRoomCommand(
            roomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId
        );

        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{roomId}")]
    public async Task<ActionResult<ApiResponse<RoomDTO?>>> GetRoomById(Guid roomId)
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var query = new GetRoomByIdQuery(
            roomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId
        );

        var result = await _mediator.Send(query);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{roomId}/unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadMessageCount(Guid roomId)
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var query = new GetUnreadMessageCountQuery(
            roomId,
            userInfo.UserType,
            userInfo.TaxUserId,
            userInfo.CustomerId
        );

        var result = await _mediator.Send(query);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{roomId}/typing")]
    public IActionResult SetTypingStatus(Guid roomId, [FromBody] TypingStatusRequest request)
    {
        return Ok(new { message = "Use WebSocket for real-time typing indicators" });
    }
}
