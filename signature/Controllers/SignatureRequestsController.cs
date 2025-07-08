using Infrastruture.Queries;
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

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var command = new GetSignatureRequestsQuery();
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var command = new GetSignatureRequestDetailQuery(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}/signers")]
    public async Task<ActionResult> GetSignersByRequestId(Guid id)
    {
        var command = new GetSignersByRequestQuery(id);
        var result = await _mediator.Send(command);
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
