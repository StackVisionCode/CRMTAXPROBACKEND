using Application.Helpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using signature.Application.DTOs;
using signature.Infrastruture.Commands;
using signature.Infrastruture.Queries;

[ApiController]
[Route("api/[controller]")]
public class SignatureRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SignatureRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("requests")]
    public async Task<ActionResult> Create([FromBody] CreateSignatureRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(new CreateSignatureRequestCommand(dto));

        if (result.Success == true)
            return CreatedAtAction(nameof(ValidateToken), new { token = "placeholder" }, result);

        return BadRequest(result);
    }

    [HttpGet("{token}")] // validar
    public async Task<ActionResult> ValidateToken(string token)
    {
        var result = await _mediator.Send(new ValidateTokenQuery(token));
        return Ok(result);
    }

    [HttpPost] // enviar firma
    public async Task<ActionResult> Submit(SignDocumentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(new SubmitSignatureCommand(dto));
        return Ok(result);
    }
}
