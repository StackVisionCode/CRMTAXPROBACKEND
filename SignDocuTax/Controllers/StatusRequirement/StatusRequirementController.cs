using Commands.StatusRequirement;
using DTOs.StatusRequiremtDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.StatusRequirement;

namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusRequirementController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StatusRequirementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStatusRequirementDto dto)
        {
            var result = await _mediator.Send(new CreateStatusRequirementCommand(dto));
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateStatusRequirementDto dto)
        {
            var result = await _mediator.Send(new UpdateStatusRequirementCommand(dto));
            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteStatusRequirementDto dto)
        {
            var result = await _mediator.Send(new DeleteStatusRequirementCommand(dto));
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAlllStatusRequirementQuery());
            return Ok(result);
        }
    }
}
