using Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReactiveEmailController : ControllerBase
{
    private readonly IReactiveEmailReceivingService _reactiveService;
    private readonly ILogger<ReactiveEmailController> _logger;

    public ReactiveEmailController(
        IReactiveEmailReceivingService reactiveService,
        ILogger<ReactiveEmailController> logger
    )
    {
        _reactiveService = reactiveService;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza TODOS los emails de una configuración (no solo no leídos)
    /// </summary>
    [HttpPost("sync/{configId:Guid}")]
    public async Task<ActionResult<ApiResponse<EmailSyncResult>>> SyncAllEmails(
        Guid configId,
        [FromQuery] DateTime? since = null,
        [FromQuery] bool forceFullSync = false
    )
    {
        try
        {
            _logger.LogInformation("📧 Manual sync request for config {ConfigId}", configId);

            if (forceFullSync)
            {
                since = DateTime.UtcNow.AddDays(-30); // Últimos 30 días
                _logger.LogInformation("🔄 Force full sync: checking last 30 days");
            }

            var result = await _reactiveService.SyncAllEmailsAsync(configId, since);

            var response = new ApiResponse<EmailSyncResult>(result.Success, result.Message, result);

            return result.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error syncing emails for config {ConfigId}", configId);
            var response = new ApiResponse<EmailSyncResult>(false, ex.Message, null);
            return StatusCode(500, response);
        }
    }

    /// <summary>
    /// Inicia el watching reactivo para una configuración específica
    /// </summary>
    [HttpPost("start-watching/{configId:Guid}")]
    public async Task<ActionResult<ApiResponse<object>>> StartWatching(Guid configId)
    {
        try
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<Infrastructure.Context.EmailContext>();

            var config = await context.EmailConfigs.FindAsync(configId);
            if (config == null)
            {
                var notFoundResponse = new ApiResponse<object>(
                    false,
                    "Configuration not found",
                    null
                );
                return NotFound(notFoundResponse);
            }

            await _reactiveService.StartWatchingAsync(config);

            var response = new ApiResponse<object>(
                true,
                $"Started reactive watching for {config.Name}",
                new { ConfigId = configId, ConfigName = config.Name }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error starting watch for config {ConfigId}", configId);
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    /// <summary>
    /// Detiene el watching reactivo para una configuración específica
    /// </summary>
    [HttpPost("stop-watching/{configId:Guid}")]
    public async Task<ActionResult<ApiResponse<object>>> StopWatching(Guid configId)
    {
        try
        {
            await _reactiveService.StopWatchingAsync(configId);

            var response = new ApiResponse<object>(
                true,
                "Stopped reactive watching",
                new { ConfigId = configId }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping watch for config {ConfigId}", configId);
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    /// <summary>
    /// Obtiene el estado del watching reactivo
    /// </summary>
    [HttpGet("watching-status")]
    public ActionResult<ApiResponse<object>> GetWatchingStatus()
    {
        try
        {
            // Esta información se mantiene en memoria estática en ReactiveEmailReceivingService
            var response = new ApiResponse<object>(
                true,
                "Watching status retrieved",
                new
                {
                    Message = "Check logs for detailed watching status",
                    Timestamp = DateTime.UtcNow,
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting watching status");
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    /// <summary>
    /// Busca emails en un rango específico (útil para testing)
    /// </summary>
    [HttpPost("search/{configId:Guid}")]
    public async Task<ActionResult<ApiResponse<object>>> SearchEmails(
        Guid configId,
        [FromQuery] DateTime? since = null,
        [FromQuery] DateTime? until = null,
        [FromQuery] int maxResults = 50
    )
    {
        try
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<Infrastructure.Context.EmailContext>();

            var config = await context.EmailConfigs.FindAsync(configId);
            if (config == null)
            {
                var notFoundResponse = new ApiResponse<object>(
                    false,
                    "Configuration not found",
                    null
                );
                return NotFound(notFoundResponse);
            }

            var emails = await _reactiveService.CheckAllEmailsAsync(config, maxResults, since);

            var response = new ApiResponse<object>(
                true,
                $"Found {emails.Count()} emails",
                new
                {
                    ConfigId = configId,
                    ConfigName = config.Name,
                    EmailsFound = emails.Count(),
                    SearchCriteria = new
                    {
                        Since = since,
                        Until = until,
                        MaxResults = maxResults,
                    },
                    Emails = emails
                        .Take(10)
                        .Select(e => new
                        {
                            e.Subject,
                            e.FromAddress,
                            e.ReceivedOn,
                            e.MessageId,
                        }),
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error searching emails for config {ConfigId}", configId);
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }
}
