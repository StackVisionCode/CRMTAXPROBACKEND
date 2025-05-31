using Common;
using CustomerService.Commands.MaritalStatusCommands;
using CustomerService.DTOs.MaritalStatusDTOs;
using CustomerService.Queries.MaritalStatusDto;
using CustomerService.Queries.MaritalStatusQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.MaritalStatus
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaritalStatusController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MaritalStatusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreateMaritalStatusDTO maritalStatusDto
        )
        {
            var command = new CreateMaritalStatusCommands(maritalStatusDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a maritalStatus" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllMaritalStatusQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdMaritalStatusQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
