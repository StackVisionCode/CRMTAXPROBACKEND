using Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [FromQuery] Guid companyId, // NUEVO: CompanyId obligatorio
        [FromQuery] DateTime? since = null,
        [FromQuery] bool forceFullSync = false
    )
    {
        try
        {
            _logger.LogInformation(
                "📧 Manual sync request for config {ConfigId} (Company: {CompanyId})",
                configId,
                companyId
            );

            if (forceFullSync)
            {
                since = DateTime.UtcNow.AddDays(-30); // Últimos 30 días
                _logger.LogInformation("🔄 Force full sync: checking last 30 days");
            }

            // ACTUALIZADO: Usar servicio con CompanyId
            var result = await _reactiveService.SyncAllEmailsAsync(configId, companyId, since);

            var response = new ApiResponse<EmailSyncResult>(result.Success, result.Message, result);

            return result.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error syncing emails for config {ConfigId} (Company: {CompanyId})",
                configId,
                companyId
            );
            var response = new ApiResponse<EmailSyncResult>(false, ex.Message, null);
            return StatusCode(500, response);
        }
    }

    /// <summary>
    /// Inicia el watching reactivo para una configuración específica
    /// </summary>
    [HttpPost("start-watching/{configId:Guid}")]
    public async Task<ActionResult<ApiResponse<object>>> StartWatching(
        Guid configId,
        [FromQuery] Guid companyId // NUEVO: CompanyId obligatorio
    )
    {
        try
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<Infrastructure.Context.EmailContext>();

            // ACTUALIZADO: Buscar config con CompanyId para seguridad
            var config = await context
                .EmailConfigs.Where(c => c.Id == configId && c.CompanyId == companyId && c.IsActive)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                var notFoundResponse = new ApiResponse<object>(
                    false,
                    "Configuration not found or access denied",
                    null
                );
                return NotFound(notFoundResponse);
            }

            await _reactiveService.StartWatchingAsync(config);

            var response = new ApiResponse<object>(
                true,
                $"Started reactive watching for {config.Name}",
                new
                {
                    ConfigId = configId,
                    CompanyId = companyId,
                    ConfigName = config.Name,
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error starting watch for config {ConfigId} (Company: {CompanyId})",
                configId,
                companyId
            );
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    /// <summary>
    /// Detiene el watching reactivo para una configuración específica
    /// </summary>
    [HttpPost("stop-watching/{configId:Guid}")]
    public async Task<ActionResult<ApiResponse<object>>> StopWatching(
        Guid configId,
        [FromQuery] Guid companyId // NUEVO: CompanyId obligatorio
    )
    {
        try
        {
            // ACTUALIZADO: Validar que el config pertenece a la compañía
            using var scope = HttpContext.RequestServices.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<Infrastructure.Context.EmailContext>();

            var configExists = await context
                .EmailConfigs.Where(c => c.Id == configId && c.CompanyId == companyId)
                .AnyAsync();

            if (!configExists)
            {
                var notFoundResponse = new ApiResponse<object>(
                    false,
                    "Configuration not found or access denied",
                    null
                );
                return NotFound(notFoundResponse);
            }

            await _reactiveService.StopWatchingAsync(configId);

            var response = new ApiResponse<object>(
                true,
                "Stopped reactive watching",
                new { ConfigId = configId, CompanyId = companyId }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error stopping watch for config {ConfigId} (Company: {CompanyId})",
                configId,
                companyId
            );
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    /// <summary>
    /// Obtiene el estado del watching reactivo
    /// </summary>
    [HttpGet("watching-status")]
    public ActionResult<ApiResponse<object>> GetWatchingStatus(
        [FromQuery] Guid? companyId = null // NUEVO: CompanyId opcional para filtrar
    )
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
                    CompanyId = companyId,
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
        [FromQuery] Guid companyId, // NUEVO: CompanyId obligatorio
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

            // ACTUALIZADO: Buscar config con CompanyId para seguridad
            var config = await context
                .EmailConfigs.Where(c => c.Id == configId && c.CompanyId == companyId && c.IsActive)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                var notFoundResponse = new ApiResponse<object>(
                    false,
                    "Configuration not found or access denied",
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
                    CompanyId = companyId,
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
            _logger.LogError(
                ex,
                "❌ Error searching emails for config {ConfigId} (Company: {CompanyId})",
                configId,
                companyId
            );
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }
}

// Clases auxiliares sin cambios
public class PubSubMessage
{
    public Message? Message { get; set; }
}

public class Message
{
    public string? Data { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}

public class GmailNotification
{
    public string? EmailAddress { get; set; }
    public long? HistoryId { get; set; }
}
