

using Application.Dtos;
using Infrastructure.Command.Templates;
using Infrastructure.Queries.Templates;
using Infrastructure.Querys.Templates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Application.Controllers.Templates;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Endpoint to create a template
    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateDto templateDto)
    {
        var result = await _mediator.Send(new CreateTemplateCommads(templateDto));
        return Ok(result);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTempladeDto dto)
    {
        var result = await _mediator.Send(new UpdateTemplateCommands(id, dto));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteTemplateCommand(id));

        if (result.Data)
            return BadRequest(result.Message);

        return Ok(result);
    }
    
    [HttpGet("owner/{ownerUserId}")]
public async Task<IActionResult> GetAllByOwner(Guid ownerUserId)
{
    var result = await _mediator.Send(new GetAllTemplatesQuery(ownerUserId));
    return Ok(result);
}

[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _mediator.Send(new GetTemplateByIdQuery(id));

    if (result.Success == false)
        return NotFound(result.Message);

    return Ok(result);
}

}