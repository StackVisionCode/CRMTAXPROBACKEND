using Commands.SignatureTypes;
using Common;
using Dtos.SignatureTypeDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.SignatureTypes;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignatureTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public SignatureTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSignatureTypeDto signatureType)
    {
        var result = await _mediator.Send(new CreateSignatureTypeCommand(signatureType));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllSignatureTypeQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {

        var command = new GetSignatureTypeByIdQuery(new GetByIdSignatureTypeDto { Id = id });
        var result = await _mediator.Send(command);

        if (!result.Success.GetValueOrDefault()) return BadRequest(result);

        return Ok(result);

    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UpdateSignatureTypeDto signatureType)
    {
        var result = await _mediator.Send(new UpdateSignatureTypeCommand(signatureType));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteSignatureTypeCommand(new DeleteSignatureTypeDto { Id = id });
        var result = await _mediator.Send(command);
        if (!result.Success.GetValueOrDefault()) return BadRequest(result);
        return Ok(result);
    }
}
