using Commands.Firms;
using DTOs.FirmsDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.Firms;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirmController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FirmController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateFirmDto dto)
        {
            var result = await _mediator.Send(new CreateFirmCommand(dto));

            if (result.Success.GetValueOrDefault())
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] UpdateFirmDto dto)
        {
            var result = await _mediator.Send(new UpdateFirmCommand(dto));
            return Ok(result);
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete([FromQuery] int id)
        {
            var result = await _mediator.Send(new DeleteFirmCommand(new DeleteFirmDto { Id = id }));
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllFirmsQuery());
            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<IActionResult> GetById([FromQuery] int id)
        {
            var result = await _mediator.Send(new GetFirmByIdQuery(id));
            return Ok(result);
        }
    }
}
