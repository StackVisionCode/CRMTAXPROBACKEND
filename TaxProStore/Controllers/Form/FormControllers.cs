

using Application.Dtos;
using Application.Dtos.Form;
using Infrastructure.Command.Form;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Querys.Form;
namespace Application.Controllers.Form;

[ApiController]
[Route("api/[controller]")]

public class FormControllers : ControllerBase
{
    private readonly IMediator _mediator;

    public FormControllers(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Endpoint to create a form
     [HttpPost ("create")]
    public async Task<IActionResult> Create([FromBody] CreateFormInstanceDto dto)
    {
        var result = await _mediator.Send(new CreateFormIntanceCommads(dto));
        return Ok(result);
    }

    [HttpGet ("get-all")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllFormInstancesQuery());
        return Ok(result);
    }
}