using Common;
using CustomerService.Commands.FilingStatusCommands;
using CustomerService.DTOs.FilingStatusDTOs;
using CustomerService.Queries.FilingStatusQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.FilingStatus
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilingStatusController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FilingStatusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreateFilingStatusDTO filingStatusDto
        )
        {
            var command = new CreateFilingStatusCommands(filingStatusDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a filingStatus" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllFilingStatusQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdFilingStatusQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
