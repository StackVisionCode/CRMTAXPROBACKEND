using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Authorizations;

namespace CustomerService.Controllers.ContactInfo
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactInfoController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ContactInfoController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HasPermission("Customer.Create")]
        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreateContactInfoDTOs contactInfo
        )
        {
            var command = new CreateContactInfoCommands(contactInfo);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a contactInfo" });
            return Ok(result);
        }

        [HasPermission("Customer.EnableLogin")]
        [HttpPost("EnableLogin")]
        public async Task<ActionResult<ApiResponse<bool>>> EnableLogin(
            [FromBody] EnableLoginDTO dto
        )
        {
            var command = new EnableCustomerLoginCommand(dto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to enable login" });
            return Ok(result);
        }

        [HasPermission("Customer.DisableLogin")]
        [HttpPost("DisableLogin")]
        public async Task<ActionResult<ApiResponse<bool>>> DisableLogin(
            [FromBody] DisableLoginDTO dto
        )
        {
            var command = new DisableCustomerLoginCommand(dto.CustomerId);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to disable login" });
            return Ok(result);
        }

        [HasPermission("Customer.Update")]
        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(
            [FromBody] UpdateContactInfoDTOs contactInfo
        )
        {
            var command = new UpdateContactInfoCommands(contactInfo);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        [HasPermission("Customer.Delete")]
        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteContactInfoCommands(id);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        [HasPermission("Customer.Read")]
        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllContactInfoQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HasPermission("Customer.Read")]
        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdContactInfoQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("Internal/AuthInfo")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<AuthInfoDTO>>> GetAuthInfo(
            [FromQuery] string email
        )
        {
            var command = new GetAuthInfoByEmailQuery(email);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("Internal/Profile")]
        public async Task<ActionResult<ApiResponse<CustomerProfileDTO>>> Profile(
            [FromQuery] Guid customerId
        )
        {
            var command = new GetCustomerProfileQuery(customerId);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
