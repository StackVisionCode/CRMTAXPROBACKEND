// Controllers/CalendarController.cs
using Application.DTO;
using Applications.DTO;
using Domain.Entities;
using Infrastructure.Commands;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CalendarService.Controllers;
[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
   private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CalendarEventDto dto)
    {
        var command = new CreateCalendarEventCommand(dto);
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update([FromBody] CalendarEventDtoGeneral dto)
    {
        var command = new UpdateCalendarEventCommand (dto);
        var success = await _mediator.Send(command);
        if (!success) return NotFound();
        return Ok("Updated");
    }

    [HttpGet("by-user")]
    public async Task<IActionResult> GetByUser(string user)
    {

        var query = new GetCalendarEventByUserQuery(Guid.Parse(user));
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var query = new GetCalendarEventByDateQuery(date);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("by-type")]
    public async Task<IActionResult> GetByType(string type)
    {
        var query = new GetCalendarEventByTypeQuery(type);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // [HttpDelete("delete/{id}")]
    // public async Task<IActionResult> Delete(Guid id)
    // {
    //     // Aquí puedes implementar un DeleteCalendarEventCommand si deseas
    //     return NoContent();
    // }
}
