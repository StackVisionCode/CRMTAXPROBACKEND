using Commands.SignatureEventType;
using DTOs.SignatureEventTypeDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.SignatureEventType;

namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignatureEventTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SignatureEventTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSignatureEventTypeDto dto)
        {
            var result = await _mediator.Send(new CreateSignatureEventTypeCommand(dto));
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSignatureEventTypeDto dto)
        {
            var result = await _mediator.Send(new UpdateSignatureEventTypeCommand(dto));
            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteSignatureEventTypeDto dto)
        {
            var result = await _mediator.Send(new DeleteSignatureEventTypeCommand(dto));
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllSignatureEventTypeQuery());
            return Ok(result);
        }
    }
}
