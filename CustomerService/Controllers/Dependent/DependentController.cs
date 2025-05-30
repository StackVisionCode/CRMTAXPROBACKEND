using Common;
using CustomerService.Commands.DependentCommands;
using CustomerService.DTOs.DependentDTOs;
using CustomerService.Queries.DependentQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.Dependent
{
    [ApiController]
    [Route("api/[controller]")]
    public class DependentController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DependentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateDependentDTO dependentDto)
        {
            var command = new CreateDependentCommands(dependentDto);
            var result = await _mediator.Send(command);
            if (result == null) return BadRequest(new { message = "Failed to create a dependent" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllDependentQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdDependentQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }
    }
}