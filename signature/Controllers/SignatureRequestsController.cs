using Application.Helpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]

public class SignatureRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SignatureRequestsController(IMediator mediator) => _mediator = mediator;


    [HttpPost("requests")]
    public async Task<ActionResult> Create([FromBody] CreateSignatureRequestDto dto)
    {
        var created = await _mediator.Send(new CreateSignatureRequestCommand(dto));
        return CreatedAtRoute("Create Signature", new { id = created.Success }, created);
    }

    [HttpGet("{token}")] // validar
    public async Task<ActionResult> Validate(string token)
    {
        var created = await _mediator.Send(new ValidateTokenQuery(token));
        return CreatedAtRoute("Create Signature", new { id = created.Success }, created);
    }

    [HttpPost]  // enviar firma

    public async Task<ActionResult> Submit(SignDocumentDto dto)
    {
        var created = await _mediator.Send(new SubmitSignatureCommand(dto));
        return CreatedAtRoute("Create Signature", new { id = created.Success }, created);
    }

}


