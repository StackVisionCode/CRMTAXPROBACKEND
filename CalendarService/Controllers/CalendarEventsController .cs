// Controllers/CalendarEventsController.cs
using Application.DTO;
using Infrastructure.Commands;
using Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CalendarService.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarEventsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CalendarEventsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("events")]
    public async Task<ActionResult<Guid>> Create([FromBody] CalendarEventDto dto)
        => Ok(await _mediator.Send(new CreateCalendarEventCommand(dto)));

    [HttpPut("events/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CalendarEventDto dto)
    {
        var ok = await _mediator.Send(new UpdateCalendarEventCommand(
            new CalendarEventUpdateDto { Id = id, Event = dto }));
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("users/{userId:guid}/events")]
    public async Task<ActionResult<List<CalendarEventDto>>> ByUser(Guid userId)
        => Ok(await _mediator.Send(new GetCalendarEventByUserQuery(userId)));
}
