using Common;
using CustomerService.Coommands.RelationshipCommands;
using CustomerService.DTOs.RelationshipDTOs;
using CustomerService.Queries.RelationshipQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.Relationship
{
    [ApiController]
    [Route("api/[controller]")]
    public class RelationshipController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RelationshipController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreateRelationshipDTO relationshipDto
        )
        {
            var command = new CreateRelationshipCommands(relationshipDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a relationship" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllRelationshipQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdRelationshipQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
