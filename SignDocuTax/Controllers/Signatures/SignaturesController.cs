using Commands.Signatures;
using DTOs.Signatures;
using Infraestructure.Commands.Signatures;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.Signatures;


namespace Controllers;

[ApiController]
[Route("api/signatures")]
public class SignaturesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SignaturesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("start-process")]
    public async Task<IActionResult> StartSigningProcess(
        [FromBody] StartSigningProcessDto dto)
    {
        var command = new StartMixedSigningProcessCommand(dto);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    [HttpPost("register-event")]
    public async Task<IActionResult> RegisterSignatureEvent(
        [FromBody] CreateSignatureEventDto dto)
    {
        var command = new CreateSignatureEventCommand(dto);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    [HttpGet("document-events/{documentId}")]
    public async Task<IActionResult> GetDocumentSignatureEvents(int documentId)
    {
        var query = new GetSignatureEventsQuery(documentId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
    
    [HttpPost("sign-document")]
public async Task<IActionResult> SignDocument([FromBody] SignPdfCommand command)
{
    var signedPath = await _mediator.Send(command);
    var fileBytes = await System.IO.File.ReadAllBytesAsync(signedPath);
    var base64 = Convert.ToBase64String(fileBytes);

    return Ok(new
    {
        message = "Documento firmado exitosamente",
        signedPath,
        base64Preview = $"data:application/pdf;base64,{base64}"
    });
}
}