using Common;
using CustomerService.Coommands.OccupationCommands;
using CustomerService.DTOs.OccupationDTOs;
using CustomerService.Queries.OccupationQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.Occupation
{
    [ApiController]
    [Route("api/[controller]")]
    public class OccupationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OccupationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreateOccupationDTO occupationDto
        )
        {
            var command = new CreateOccupationCommands(occupationDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create an occupation" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllOccupationQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdOccupationQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
