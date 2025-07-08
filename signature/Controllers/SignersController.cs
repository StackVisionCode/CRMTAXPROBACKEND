using Infrastruture.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SignersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SignersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var command = new GetSignerDetailQuery(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
