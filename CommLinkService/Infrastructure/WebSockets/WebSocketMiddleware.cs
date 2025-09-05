using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using CommLinkService.Application.Common.Utils;
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
        ILogger<WebSocketMiddleware> logger
    )
    {
        _next = next;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    string? GetUserId(ClaimsPrincipal user)
    {
        // 1) NameIdentifier mapeado (si existe)
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(id))
            return id;

        // 2) sub (lo fijaste como NameClaimType en TokenValidationParameters)
        id = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!string.IsNullOrEmpty(id))
            return id;

        // 3) nameid "crudo" (por si el handler no mapeó)
        id = user.FindFirst("nameid")?.Value;
        return id;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }

        var userIdStr = GetUserId(context.User);
        var companyIdStr = context.User.FindFirst("companyId")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("WebSocket connection attempt without valid user authentication.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Guid? companyId = null;
        if (
            !string.IsNullOrEmpty(companyIdStr)
            && Guid.TryParse(companyIdStr, out var tempCompanyId)
        )
        {
            companyId = tempCompanyId;
        }

        ParticipantType userType;
        Guid? taxUserId = null;
        Guid? customerId = null;

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            // Es un TaxUser
            userType = ParticipantType.TaxUser;
            taxUserId = userId;
        }
        else
        {
            // Es un Customer
            userType = ParticipantType.Customer;
            customerId = userId;
            companyId = null; // Los customers no tienen companyId
        }

        var connectionId = Guid.NewGuid().ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var ipAddress = IpAddressHelper.GetClientIp(context);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        await _webSocketManager.AddConnectionAsync(
            userType,
            taxUserId,
            customerId,
            companyId,
            connectionId,
            webSocket,
            userAgent,
            ipAddress
        );

        try
        {
            await HandleWebSocketAsync(
                context,
                webSocket,
                userType,
                taxUserId,
                customerId,
                companyId,
                connectionId,
                serviceProvider
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error for connection {ConnectionId}", connectionId);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
        finally
        {
            await _webSocketManager.RemoveConnectionAsync(connectionId);
        }
    }

    private async Task HandleWebSocketAsync(
        HttpContext context,
        WebSocket webSocket,
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        Guid? companyId,
        string connectionId,
        IServiceProvider serviceProvider
    )
    {
        var buffer = new byte[1024 * 8];
        var messageHandler = new WebSocketMessageHandler(
            serviceProvider,
            _webSocketManager,
            _logger
        );

        await _webSocketManager.SendToConnectionAsync(
            connectionId,
            new
            {
                type = "connected",
                data = new
                {
                    connectionId,
                    userType,
                    taxUserId,
                    customerId,
                    companyId,
                },
            }
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

                    await messageHandler.HandleMessageAsync(
                        userType,
                        taxUserId,
                        customerId,
                        companyId,
                        connectionId,
                        fullMessage
                    );
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
