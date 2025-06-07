using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreateCustomerDTO customer
        )
        {
            var command = new CreateCustomerCommands(customer);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a customer" });
            return Ok(result);
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(
            [FromBody] UpdateCustomerDTO customer
        )
        {
            var command = new UpdateCustomerCommands(customer);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteCustomerCommands(id);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllCustomerQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetOwnCustomers")]
        public async Task<ActionResult> GetOwnCustomers()
        {
            // 1) extraer el Id de usuario (claim "sub" ó NameIdentifier)
            var rawId =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(rawId, out var userId))
                return Unauthorized(
                    new ApiResponse<List<ReadCustomerDTO>>(false, "Invalid session")
                );

            var command = new GetOwnCustomersQueries(userId);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdCustomerQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
