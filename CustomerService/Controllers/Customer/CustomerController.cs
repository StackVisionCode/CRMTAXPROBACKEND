using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Common;
using CustomerService.Commands.CustomerCommands;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Authorizations;

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

        // [HasPermission("Customer.Create")]
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

        // [HasPermission("Customer.Update")]
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

        // [HasPermission("Customer.Delete")]
        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteCustomerCommands(id);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        // Ahora requiere CompanyId del token
        // [HasPermission("Customer.Read")]
        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            // Extraer CompanyId del token
            var companyIdClaim = User.FindFirstValue("companyId");
            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized(
                    new ApiResponse<List<ReadCustomerDTO>>(false, "Invalid company session")
                );
            }

            var command = new GetCustomersByCompanyQueries(companyId);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        // Usa CompanyId del token
        // [HasPermission("Customer.Read")]
        [HttpGet("GetOwnCustomers")]
        public async Task<ActionResult> GetOwnCustomers([FromQuery] bool? onlyMine = null)
        {
            // Extraer CompanyId del token
            var companyIdClaim = User.FindFirstValue("companyId");
            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized(
                    new ApiResponse<List<ReadCustomerDTO>>(false, "Invalid company session")
                );
            }

            // Si onlyMine=true, filtrar por el TaxUserId actual
            Guid? createdByFilter = null;
            if (onlyMine == true)
            {
                var userIdClaim =
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    createdByFilter = userId;
                }
            }

            var command = new GetOwnCustomersQueries(companyId, createdByFilter);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        // [HasPermission("Customer.Read")]
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
