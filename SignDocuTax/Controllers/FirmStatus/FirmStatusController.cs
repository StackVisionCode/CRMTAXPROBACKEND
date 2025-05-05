using Common;
using Commands.FirmStatus;
using Dtos.FirmsStatusDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.FirmStatus;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirmStatusController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FirmStatusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/FirmStatus
        [HttpPost]
        public async Task<ActionResult<ApiResponse<bool>>> CreateFirmStatus([FromBody] CreateFirmStatusDto createFirmStatusDto)
        {
            var command = new CreateFirmStatusCommand(createFirmStatusDto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // PUT: api/FirmStatus/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateFirmStatus([FromBody] UpdateFirmStatusDto updateFirmStatusDto)
        {
            var command = new UpdateFirmStatusCommand(updateFirmStatusDto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // DELETE: api/FirmStatus/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteFirmStatus(int id)
        {
            var command = new DeleteFirmStatusCommand(new DeleteFirmStatusDto{Id = id });
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // GET: api/FirmStatus
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<FirmStatusDto>>>> GetAllFirmStatus()
        {
            var query = new GetAllFirmStatusQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // GET: api/FirmStatus/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<FirmStatusDto>>> GetFirmStatusById(int id)
        {
            var query = new GetFirmStatusByIdQuery(new ResponseInfo {Id=id});
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
