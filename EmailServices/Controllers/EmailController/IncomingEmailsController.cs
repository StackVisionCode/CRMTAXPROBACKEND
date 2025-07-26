using Application.Common.DTO;
using Common;
using Infrastructure.Commands;
using Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncomingEmailsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IncomingEmailsController(IMediator mediator) => _mediator = mediator;

    // Listar emails entrantes
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<IncomingEmailDTO>>>> List(
        [FromQuery] Guid? userId = null,
        [FromQuery] bool? isRead = null
    )
    {
        try
        {
            var result = await _mediator.Send(new GetIncomingEmailsQuery(userId, isRead));
            var response = new ApiResponse<IEnumerable<IncomingEmailDTO>>(
                true,
                "Incoming emails retrieved successfully",
                result
            );
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<IEnumerable<IncomingEmailDTO>>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Obtener email entrante por ID
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<ApiResponse<IncomingEmailDTO>>> Get(Guid id)
    {
        try
        {
            var email = await _mediator.Send(new GetIncomingEmailByIdQuery(id));
            if (email is null)
            {
                var notFoundResponse = new ApiResponse<IncomingEmailDTO>(
                    false,
                    "Email not found",
                    null
                );
                return NotFound(notFoundResponse);
            }

            var response = new ApiResponse<IncomingEmailDTO>(
                true,
                "Email retrieved successfully",
                email
            );
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<IncomingEmailDTO>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Marcar como leído
    [HttpPatch("{id:Guid}/mark-read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(Guid id)
    {
        try
        {
            await _mediator.Send(new MarkIncomingEmailAsReadCommand(id));
            var response = new ApiResponse<object>(true, "Email marked as read", null);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }
}
