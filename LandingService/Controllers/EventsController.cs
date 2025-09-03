using LandingService.Applications.DTO;
using LandingService.Infrastructure.Commands;
using LandingService.Infrastructure.queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LandingService.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("CreateEvent")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        var command = new CreateEventCommand(dto);
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetEventById), new { id }, null);
    }

    [HttpGet("GetEventById")]
    public async Task<ActionResult<CreateEventDto>> GetEventById(Guid id)
    {
        var query = new GetEventByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

