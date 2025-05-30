using Common;
using CustomerService.Commands.CustomerCommands;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.Customer
{
  [ApiController]
  [Route("api/[controller]")]
  public class CustomerController : ControllerBase
  {
    private readonly IMediator _mediator;
    public CustomerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateCustomerDTO customerDto)
    {
      var command = new CreateCustomerCommands(customerDto);
      var result = await _mediator.Send(command);
      if (result == null) return BadRequest(new { message = "Failed to create a customer" });
      return Ok(result);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult> GetAll()
    {
      var command = new GetAllCustomerQueries();
      var result = await _mediator.Send(command);
      if (result.Success == false) return BadRequest(new { result });

      return Ok(result);
    }
  }
}