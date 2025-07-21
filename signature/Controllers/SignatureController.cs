using Infrastructure.Queries.CustomerSignatureQueries;
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
        private readonly ILogger<SignatureController> _logger;

        public SignatureController(IMediator mediator, ILogger<SignatureController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las solicitudes de firma de un cliente específico
        /// </summary>
        /// <param name="customerId">ID del cliente</param>
        [HttpGet("{customerId}/requests")]
        public async Task<ActionResult> GetCustomerRequests(Guid customerId)
        {
            _logger.LogInformation(
                "Obteniendo solicitudes de firma para cliente {CustomerId}",
                customerId
            );

            var result = await _mediator.Send(new GetCustomerSignatureRequestsQuery(customerId));

            if (result.Success == false)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene el historial completo de firmas de un cliente
        /// </summary>
        /// <param name="customerId">ID del cliente</param>
        [HttpGet("{customerId}/history")]
        public async Task<ActionResult> GetCustomerHistory(Guid customerId)
        {
            _logger.LogInformation(
                "Obteniendo historial de firmas para cliente {CustomerId}",
                customerId
            );

            var result = await _mediator.Send(new GetCustomerSignatureHistoryQuery(customerId));

            if (result.Success == false)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene la actividad de una solicitud específica
        /// </summary>
        /// <param name="requestId">ID de la solicitud</param>
        [HttpGet("requests/{requestId}/activity")]
        public async Task<ActionResult> GetRequestActivity(Guid requestId)
        {
            _logger.LogInformation("Obteniendo actividad para solicitud {RequestId}", requestId);

            var result = await _mediator.Send(new GetSignatureActivityQuery(requestId));

            if (result.Success == false)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene estadísticas de un cliente específico
        /// </summary>
        /// <param name="customerId">ID del cliente</param>
        [HttpGet("{customerId}/stats")]
        public async Task<ActionResult> GetCustomerStats(Guid customerId)
        {
            _logger.LogInformation("Obteniendo estadísticas para cliente {CustomerId}", customerId);

            var result = await _mediator.Send(new GetCustomerSignatureStatsQuery(customerId));

            if (result.Success == false)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene solicitudes urgentes de un cliente
        /// </summary>
        /// <param name="customerId">ID del cliente</param>
        [HttpGet("{customerId}/urgent")]
        public async Task<ActionResult> GetUrgentRequests(Guid customerId)
        {
            _logger.LogInformation(
                "Obteniendo solicitudes urgentes para cliente {CustomerId}",
                customerId
            );

            var result = await _mediator.Send(new GetUrgentSignatureRequestsQuery(customerId));

            if (result.Success == false)
            {
                return BadRequest(result);
            }

            return Ok(result);
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
