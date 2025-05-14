using Commands.Signers;
using DTOs.Documents;
using DTOs.Signers;
<<<<<<< HEAD
using MediatR;
using Microsoft.AspNetCore.Mvc;
=======
using Handlers.Signers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.Signers;
>>>>>>> 4b49bd843ef322600271ae0810b969304e69192e

namespace Controllers.DocumentsSigner
{
    [ApiController]
    [Route("api/[controller]")]

    public class DocumentsSignerControllers : ControllerBase
    {
        private readonly IMediator _mediator;
        public DocumentsSignerControllers(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateExternalSignerDto dto)
        {
            var result = await _mediator.Send(new CreateExternalSignerCommand(dto));
            return Ok(result);
        }


        [HttpPost("with-signers")]
        public async Task<IActionResult> CreateDocumentWithSigners([FromForm] CreateDocumentWithSignersRequest request)
        {
            var result = await _mediator.Send(new CreateDocumentWithSignersCommand(request));
            if (!result.Success.GetValueOrDefault()) return BadRequest(result);

            return Ok(result);
        }

        // === Firmas ===

        [HttpPost("sign")]
        public async Task<IActionResult> SignDocument([FromBody] SignDocumentRequest request)
        {
            // Autocompletar datos de auditoría
            request.IpAddress ??= HttpContext.Connection.RemoteIpAddress?.ToString();
            request.DeviceInfo ??= HttpContext.Request.Headers["User-Agent"];

            var result = await _mediator.Send(new SignDocumentCommand(request));
            if (!result.Success.GetValueOrDefault()) return BadRequest(result);

            return Ok(result);
        }

<<<<<<< HEAD
        [HttpGet("{documentId}/signers")]
        public async Task<IActionResult> GetDocumentSigners(int documentId)
        {
            var result = await _mediator.Send(new GetDocumentSignersQuery(documentId));
            if (!result.Success.GetValueOrDefault()) return BadRequest(result);

            return Ok(result);
        }
=======
       [HttpGet("{documentId}/signers")]
    public async Task<IActionResult> GetDocumentSigners(int documentId)
    {
        var response = await _mediator.Send(new GetDocumentSignersQuery(documentId));
        return Ok(response);
    }
>>>>>>> 4b49bd843ef322600271ae0810b969304e69192e



        // === Utilitarios ===

        [HttpGet("pending/{userId}")]
        public async Task<IActionResult> GetPendingDocuments(int userId)
        {
            var result = await _mediator.Send(new GetPendingDocumentsQuery(userId));
            if (!result.Success.GetValueOrDefault()) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("external/{token}")]
        public async Task<IActionResult> GetDocumentByToken(string token)
        {
            var result = await _mediator.Send(new GetDocumentByTokenQuery(token));
            if (!result.Success.GetValueOrDefault()) return BadRequest(result);

            return Ok(result);
        }
    }
}
