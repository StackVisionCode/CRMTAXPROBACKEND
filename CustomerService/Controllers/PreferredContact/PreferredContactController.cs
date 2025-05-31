using Common;
using CustomerService.Commands.PreferredContactCommads;
using CustomerService.DTOs.PreferredContactDTOs;
using CustomerService.Queries.PreferredContactQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.PreferredContact
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreferredContactController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PreferredContactController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreatePreferredContactDTO preferredContactDto
        )
        {
            var command = new CreatePreferredContactCommands(preferredContactDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a preferredContact" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllPreferredContactQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdPreferredContactQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
