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
    public async Task<ActionResult<EmailConfigDTO>> Create([FromBody] CreateEmailConfigDTO dto)
    {
        var created = await _med.Send(
            new CreateEmailConfigCommand(dto, dto.CompanyId, dto.CreatedByTaxUserId)
        );
        return CreatedAtRoute(
            "GetEmailConfigById",
            new { id = created.Id, companyId = dto.CompanyId },
            created
        );
    }

    // update
    [HttpPut("{id:Guid}")]
    public async Task<ActionResult<EmailConfigDTO>> Update(
        Guid id,
        [FromBody] UpdateEmailConfigDTO dto
    )
    {
        var updated = await _med.Send(
            new UpdateEmailConfigCommand(id, dto, dto.CompanyId, dto.LastModifiedByTaxUserId)
        );
        return Ok(updated);
    }

    // delete
    [HttpDelete("{id:Guid}")]
    public async Task<IActionResult> Delete(Guid id, DeleteEmailConfigDTO dto)
    {
        await _med.Send(new DeleteEmailConfigCommand(id, dto.CompanyId, dto.DeletedByTaxUserId));
        return NoContent();
    }

    // list
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmailConfigDTO>>> List(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? taxUserId = null
    )
    {
        var list = await _med.Send(new GetEmailConfigsQuery(companyId, taxUserId));
        return Ok(list);
    }

    // detail
    [HttpGet("{id:Guid}", Name = "GetEmailConfigById")]
    public async Task<ActionResult<EmailConfigDTO>> Get(Guid id, [FromQuery] Guid companyId)
    {
        var cfg = await _med.Send(new GetEmailConfigByIdQuery(id, companyId));
        if (cfg is null)
            return NotFound();
        return Ok(cfg);
    }
}
