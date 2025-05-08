using Common;
using CustomerService.Commands.CustomerTypeCommands;
using CustomerService.DTOs.CustomerDTOs;
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

    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CustomerTypeDTO customerTypeDto)
    {
      var command = new CreateCustomerTypeCommands(customerTypeDto);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to create a customer type" });
      return Ok(result);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult> GetAll()
    {
      var command = new GetAllCustomerTypeQueries();
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }
  }
}