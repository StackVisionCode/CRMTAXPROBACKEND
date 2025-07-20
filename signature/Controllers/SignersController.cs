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

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var command = new GetSignersQuery();
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// </summary>
    /// <param name="token">Token de firma del firmante</param>
    /// <returns>Estado actual del firmante y si puede proceder</returns>
    [HttpGet("status/{token}")]
    public async Task<IActionResult> CheckSignerStatus(string token)
    {
        var query = new CheckSignerStatusQuery(token);
        var result = await _mediator.Send(query);

        if (result.Success.HasValue && !result.Success.Value)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var command = new GetSignerDetailQuery(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
