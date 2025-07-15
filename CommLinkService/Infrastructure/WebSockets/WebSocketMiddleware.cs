using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using CommLinkService.Infrastructure.Services;

namespace CommLinkService.Infrastructure.WebSockets;

public sealed class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(
        RequestDelegate next,
        IWebSocketManager webSocketManager,
        ILogger<WebSocketMiddleware> logger,
        IConfiguration configuration
    )
    {
        _next = next;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    // *******************************************************************
    // ** ESTE ES EL MÉTODO PÚBLICO QUE FALTABA **
    // *******************************************************************
    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        // Si no es una solicitud de WebSocket, pasa al siguiente middleware.
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }

        // Validar autenticación
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("WebSocket connection attempt without authentication.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userGuid = Guid.Parse(userId);
        var connectionId = Guid.NewGuid().ToString();

        // Aceptar la conexión WebSocket
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await _webSocketManager.AddConnectionAsync(userGuid, connectionId, webSocket);

        try
        {
            // Llamar a la lógica de manejo de mensajes que ya teníamos
            await HandleWebSocketAsync(context, webSocket, userGuid, connectionId, serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error for connection {ConnectionId}", connectionId);
            // Asegúrate de que la respuesta no se haya enviado ya
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
        finally
        {
            // Asegurarse de limpiar la conexión al finalizar
            await _webSocketManager.RemoveConnectionAsync(connectionId);
        }
    }

    // Este método ahora es privado y es llamado por InvokeAsync
    private async Task HandleWebSocketAsync(
        HttpContext context,
        WebSocket webSocket,
        Guid userId,
        string connectionId,
        IServiceProvider serviceProvider
    )
    {
        var buffer = new byte[1024 * 8]; // Buffer de 8KB
        var messageHandler = new WebSocketMessageHandler(
            serviceProvider,
            _webSocketManager,
            _logger
        );

        await _webSocketManager.SendToConnectionAsync(
            connectionId,
            new { type = "connected", data = new { connectionId, userId } }
        );

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    var segment = new ArraySegment<byte>(buffer);
                    result = await webSocket.ReceiveAsync(segment, CancellationToken.None);
                    ms.Write(segment.Array!, segment.Offset, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by client",
                        CancellationToken.None
                    );
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    var fullMessage = await reader.ReadToEndAsync();
                    await messageHandler.HandleMessageAsync(userId, connectionId, fullMessage);
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(
                    ex,
                    "WebSocket exception for connection {ConnectionId}. State: {State}",
                    connectionId,
                    webSocket.State
                );
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing WebSocket message for connection {ConnectionId}",
                    connectionId
                );
                await _webSocketManager.SendToConnectionAsync(
                    connectionId,
                    new
                    {
                        type = "error",
                        data = new { message = "An internal server error occurred." },
                    }
                );
            }
        }
    }
}
