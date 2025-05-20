using Application.Common.DTO;
using Infrastructure.Commands;
using Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailConfigController : ControllerBase
{
    private readonly IMediator _med;

    public EmailConfigController(IMediator med)
    {
        _med = med;
    }

    // create
    [HttpPost]
    public async Task<ActionResult<EmailConfigDTO>> Create([FromBody] EmailConfigDTO dto)
    {
        var created = await _med.Send(new CreateEmailConfigCommand(dto));
        return CreatedAtRoute("GetEmailConfigById", new { id = created.Id }, created);
    }

    // update
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmailConfigDTO>> Update(int id, [FromBody] EmailConfigDTO dto)
    {
        var updated = await _med.Send(new UpdateEmailConfigCommand(id, dto));
        return Ok(updated);
    }

    // delete
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _med.Send(new DeleteEmailConfigCommand(id));
        return NoContent();
    }

    // list
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmailConfigDTO>>> List(
        [FromQuery] int? companyId,
        [FromQuery] int? userId
    )
    {
        var list = await _med.Send(new GetEmailConfigsQuery(companyId, userId));
        return Ok(list);
    }

    // detail
    [HttpGet("{id:int}", Name = "GetEmailConfigById")]
    public async Task<ActionResult<EmailConfigDTO>> Get(int id)
    {
        var cfg = await _med.Send(new GetEmailConfigByIdQuery(id));
        if (cfg is null)
            return NotFound();
        return Ok(cfg);
    }
}
