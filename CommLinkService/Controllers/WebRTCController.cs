using CommLinkService.Application.Common.Utils;
using CommLinkService.Application.DTOs.VideoCallDTOs;
using CommLinkService.Infrastructure.Services;
using MassTransit.Futures.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CommLinkService.Controllers
{
    [ApiController]
    [Route("api/commlink/webrtc")]
    [EnableRateLimiting("WebRTCPolicy")] // Rate limiting específico
    public class WebRTCController : ControllerBase
    {
        private readonly IWebRTCService _webrtcService;
        private readonly ILogger<WebRTCController> _logger;

        public WebRTCController(IWebRTCService webrtcService, ILogger<WebRTCController> logger)
        {
            _webrtcService = webrtcService;
            _logger = logger;
        }

        /// <summary>
        /// Genera credenciales temporales para TURN server
        /// Endpoint semi-público con rate limiting
        /// </summary>
        [HttpGet("turn-credentials")]
        [ProducesResponseType(typeof(TurnCredentialsDto), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TurnCredentialsDto>> GetTurnCredentials()
        {
            try
            {
                var clientIp = IpAddressHelper.GetClientIp(HttpContext) ?? "unknown";
                _logger.LogInformation("TURN credentials requested from IP: {ClientIp}", clientIp);

                var credentials = await _webrtcService.GenerateTurnCredentialsAsync();

                return Ok(credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating TURN credentials");
                return StatusCode(500, new { error = "Failed to generate credentials" });
            }
        }

        /// <summary>
        /// Health check del TURN server
        /// </summary>
        [HttpGet("turn-health")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> GetTurnServerHealth()
        {
            try
            {
                var isHealthy = await _webrtcService.ValidateTurnServerHealthAsync();
                return Ok(new { isHealthy, checkedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking TURN server health");
                return Ok(new { isHealthy = false, checkedAt = DateTime.UtcNow });
            }
        }
    }
}
