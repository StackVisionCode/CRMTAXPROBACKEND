using Commands.Documents;
using Common;
using DTOs.Documents;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.Documents;

namespace Controllers.Documents
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] CreateDocumentCommands command)
        {
            var result = await _mediator.Send(command);

            if (result.Success.GetValueOrDefault())
                return Ok(result);

            return BadRequest(result);
        }
        [HttpGet("GetAll")]
        public async Task<ActionResult<ApiResponse<ReadDocumentsDto[]>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllDocumentsQuery());

            if (!result.Success.GetValueOrDefault()) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult<ApiResponse<ReadDocumentsDto>>> GetById([FromQuery] int id)
        {
            var command = new GetDocumentsByIdQuery(new ReadDocumentByIdDto { Id = id });
            var result = await _mediator.Send(command);

            if (!result.Success.GetValueOrDefault()) return BadRequest(result);

            return Ok(result);
        }


        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] UpdateDocumentDto documentsDto)
        {
            var command = new UpdateDocumentCommands(documentsDto);
            var result = await _mediator.Send(command);

            if (!result.Success.GetValueOrDefault()) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete([FromQuery] int  id)
        {
            var command = new DeleteDocumentCommands( new DeleteDocumentsDto {Id = id });
            var result = await _mediator.Send(command);

            if (!result.Success.GetValueOrDefault()) return BadRequest(result);

            return Ok(result);
        }
    }
}
