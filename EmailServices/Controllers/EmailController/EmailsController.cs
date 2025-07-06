using Application.Common.DTO;
using Infrastructure.Commands;
using Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailsController(IMediator mediator) => _mediator = mediator;

    // crear email (pendiente)
    [HttpPost]
    public async Task<ActionResult<EmailDTO>> Create([FromBody] EmailDTO dto)
    {
        var created = await _mediator.Send(new CreateEmailCommand(dto));
        return CreatedAtRoute("GetEmailById", new { id = created.Id }, created);
    }

    // enviar email
    [HttpPost("{id:Guid}/send")]
    public async Task<ActionResult<EmailDTO>> Send(Guid id)
    {
        var sent = await _mediator.Send(new SendEmailCommand(id, null));
        return Ok(sent);
    }

    // listar
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmailDTO>>> List([FromQuery] Guid? userId)
    {
        var result = await _mediator.Send(new GetEmailsQuery(userId));
        return Ok(result);
    }

    // detalle
    [HttpGet("{id:Guid}", Name = "GetEmailById")]
    public async Task<ActionResult<EmailDTO>> Get(Guid id)
    {
        var mail = await _mediator.Send(new GetEmailByIdQuery(id));
        if (mail is null)
            return NotFound();
        return Ok(mail);
    }
}
