using MediatR;
using Microsoft.AspNetCore.Mvc;
using signature.Application.Commands;
using signature.Application.Queries;

namespace signature.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignatureController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SignatureController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Verifica si hay preview disponible para un firmante
        /// Se llama después de firmar para verificar si el documento ya está sellado
        /// </summary>
        [HttpGet("preview/available/{signerId}")]
        public async Task<IActionResult> CheckAvailablePreview(Guid signerId)
        {
            var query = new CheckAvailablePreviewQuery(signerId);
            var result = await _mediator.Send(query);

            if (result.Success?.Equals(false) ?? true)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene información del documento para preview
        /// </summary>
        [HttpGet("preview/info")]
        public async Task<IActionResult> GetPreviewInfo(
            [FromQuery] string accessToken,
            [FromQuery] string sessionId
        )
        {
            var query = new GetPreviewInfoQuery(accessToken, sessionId);
            var result = await _mediator.Send(query);

            if (result.Success?.Equals(false) ?? true)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Verifica el estado de acceso al preview
        /// </summary>
        [HttpGet("preview/status")]
        public async Task<IActionResult> CheckPreviewStatus(
            [FromQuery] string accessToken,
            [FromQuery] string sessionId
        )
        {
            var query = new CheckPreviewStatusQuery(accessToken, sessionId);
            var result = await _mediator.Send(query);

            if (result.Success?.Equals(false) ?? true)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Registra un acceso al preview
        /// </summary>
        [HttpPost("preview/access")]
        public async Task<IActionResult> RecordPreviewAccess(
            [FromBody] RecordPreviewAccessCommand request
        )
        {
            var result = await _mediator.Send(request);

            if (result.Success?.Equals(false) ?? true)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Invalida el acceso de preview
        /// </summary>
        [HttpPost("preview/invalidate")]
        public async Task<IActionResult> InvalidatePreviewAccess(
            [FromBody] InvalidatePreviewAccessCommand request
        )
        {
            var result = await _mediator.Send(request);

            if (result.Success?.Equals(false) ?? true)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
