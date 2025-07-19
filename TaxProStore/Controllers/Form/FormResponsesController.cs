using Application.Dtos.Form;
using Infrastructure.Command.Form;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers.Form;

[ApiController]
[Route("api/[controller]")]
public class FormResponsesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FormResponsesController(IMediator mediator)
    {
        _mediator = mediator;
    }

[HttpPost("create")]
public async Task<IActionResult> CreateResponse([FromBody] FormResponseDto responseDto)
{
    var command = new CreateFormResponseCommand(responseDto);
    var result = await _mediator.Send(command);
    
        return Ok(result);
    }
    // GET: api/FormResponses
    [HttpGet ("get-all")]
    public async Task<IActionResult> GetAllResponses()
    {
        var query = new GetAllFormResponsesQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

   
}
