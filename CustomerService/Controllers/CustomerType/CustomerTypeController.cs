using Common;
using CustomerService.Queries.CustomerTypeQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.CustomerType
{
  [ApiController]
  [Route("api/[controller]")]
  public class CustomerTypeController : ControllerBase
  {
    private readonly IMediator _mediator;

    public CustomerTypeController(IMediator mediator)
    {
      _mediator = mediator;
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult> GetAll()
    {
      var command = new GetAllCustomerTypeQueries();
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }

    [HttpGet("GetById")]
    public async Task<ActionResult> GetById(Guid Id)
    {
      var command = new GetByIdCustomerTypeQueries(Id);
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }
  }
}