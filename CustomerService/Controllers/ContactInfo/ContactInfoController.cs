using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteContactInfoCommands(id);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllContactInfoQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdContactInfoQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
