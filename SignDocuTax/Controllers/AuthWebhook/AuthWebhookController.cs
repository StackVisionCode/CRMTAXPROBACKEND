using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/webhooks/auth")]
public class AuthWebhookController : ControllerBase
{
    private readonly ILogger<AuthWebhookController> _logger;
      private readonly ITokenStorage _tokenStorage;

    public AuthWebhookController(ILogger<AuthWebhookController> logger, ITokenStorage tokenStorage)
    {
        _logger = logger;
        _tokenStorage = tokenStorage;
    }

    [HttpPost("events")]
    public IActionResult HandleAuthEvent([FromBody] AuthEventRequest request)
    {
        _logger.LogInformation($"Evento recibido: {request.EventType} - User: {request.data}");

        switch (request.EventType)
        {
            case "UserAuthenticated":
                _tokenStorage.StoreToken(request);
                return Ok();
            case "TokenRevoked":
                // Lógica para invalidar tokens
                return Ok();
            default:
                return BadRequest("Evento no soportado");
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireValidToken")] // Usa la política definida
public class SecureController : ControllerBase
{
    private readonly ITokenStorage _tokenStorage;

    public SecureController(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    [HttpGet("protected-data")]
    public IActionResult GetProtectedData()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        return Ok(new { message = "Datos protegidos", userId });
    }
}

public class AuthEventRequest
{
    public string EventType { get; set; } // "UserAuthenticated", "TokenRevoked", etc.
    public object data { get; set; }
     public DateTime Expired { get; set; }
    public string? AdditionalData { get; set; }
}