using CommLinkService.Application.Commands;
using CommLinkService.Application.Common.Shared;
using CommLinkService.Application.Queries;
using Common;
using DTOs.ConnectionDTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommLinkService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConnectionController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConnectionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<ConnectionDTO>>>> GetActiveConnections()
    {
        var userInfo = UserTokenHelper.GetUserFromToken(User);
        if (userInfo == null)
            return Unauthorized();

        var query = new GetActiveConnectionsQuery(
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

    [HttpPost("disconnect/{connectionId}")]
    public async Task<ActionResult<ApiResponse<bool>>> Disconnect(string connectionId)
    {
        var command = new DisconnectCommand(connectionId);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
