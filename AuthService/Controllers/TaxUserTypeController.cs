using Commands.UserTypeCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.UserTypeQueries;
using UserDTOS;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaxUserTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TaxUserTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] TaxUserTypeDTO userTypeDto)
        {
            var command = new CreateTaxUserTypeCommands(userTypeDto);
            var result = await _mediator.Send(command);
            if (result == null) return BadRequest(new {  message = "Failed to create a user type" });
            return Ok(result);
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] TaxUserTypeDTO userTypeDto)
        {
            var command = new UpdateTaxUserTypeCommands(userTypeDto);
            var result = await _mediator.Send(command);
            if (result == null) return BadRequest(new { message = "Failed to update a user type" });
            return Ok(result);
        }

        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var command = new DeleteTaxUserTypeCommands(id);
            var result = await _mediator.Send(command);
            if (result == null) return BadRequest(new { message = "Failed to delete a user type" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllTaxUserTypeQuery();
            var result = await _mediator.Send(command);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(int id)
        {
            var command = new GetTaxUserByIdQuery(id);
            var result = await _mediator.Send(command);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }
    }
}