using Commands.Contacts;
using DTOs.Contacts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.Contacts;

namespace Controllers.COntacts;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactDto dto)
    {
        var command = new CreateContactCommand(dto);
        var result = await _mediator.Send(command);

        if (!result.Success.GetValueOrDefault()) return BadRequest(new { result });

        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateContactDto dto)
    {
        var command = new UpdateContactCommand(dto);
        var result = await _mediator.Send(command);

        if (!result.Success.GetValueOrDefault()) return BadRequest(new { result });

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllContactsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteContactCommand(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

 [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetContactByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("ByUserTaxId/{userTaxId}")]
    public async Task<IActionResult> GetByUserTaxId(int userTaxId)
    {
        var result = await _mediator.Send(new GetContactsByUserTaxIdQuery(userTaxId));
        return Ok(result);
    }

    [HttpGet("ByCompanyId/{companyId}")]
    public async Task<IActionResult> GetByCompanyId(int companyId)
    {
        var result = await _mediator.Send(new GetContactsByCompanyIdQuery(companyId));
        return Ok(result);
    }

    [HttpGet("ByName/{name}")]
    public async Task<IActionResult> GetByName(string name)
    {
        var result = await _mediator.Send(new GetContactsByNameQuery(name));
        return Ok(result);
    }
}

