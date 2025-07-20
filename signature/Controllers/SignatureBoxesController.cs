using Infrastruture.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SignatureBoxesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SignatureBoxesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var command = new GetSignatureBoxesQuery();
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
