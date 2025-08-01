using System.Text.Json;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Services;
using MediatR;

namespace CommLinkService.Infrastructure.WebSockets;

public sealed class WebSocketMessageHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger _logger;

    public WebSocketMessageHandler(
        IServiceProvider serviceProvider,
        IWebSocketManager webSocketManager,
        ILogger logger
    )
    {
        _serviceProvider = serviceProvider;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task HandleMessageAsync(Guid userId, string connectionId, string message)
    {
        try
        {
            WebSocketMessage? messageData;
            try
            {
                messageData = JsonSerializer.Deserialize<WebSocketMessage>(message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Deserialización fallida. Raw: {Message}", message);
                await _webSocketManager.SendToConnectionAsync(
                    connectionId,
                    new
                    {
                        type = "error",
                        data = new
                        {
                            message = "Mensaje no válido. Asegúrate de enviar JSON válido.",
                        },
                    }
                );
                return;
            }

            if (messageData == null || string.IsNullOrWhiteSpace(messageData.Type))
            {
                await _webSocketManager.SendToConnectionAsync(
                    connectionId,
                    new
                    {
                        type = "error",
                        data = new { message = "El mensaje debe tener un tipo definido." },
                    }
                );
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            switch (messageData.Type.ToLower())
            {
                case "join_room":
                    await HandleJoinRoom(mediator, userId, connectionId, messageData.Data);
                    break;
                case "leave_room":
                    await HandleLeaveRoom(mediator, userId, messageData.Data);
                    break;
                case "send_message":
                    await HandleSendMessage(mediator, userId, messageData.Data);
                    break;
                case "typing":
                    await HandleTyping(userId, messageData.Data);
                    break;
                case "video_signal":
                    await HandleVideoSignal(userId, messageData.Data);
                    break;
                case "ice_candidate":
                    await HandleIceCandidate(userId, messageData.Data);
                    break;
                case "ping":
                    await _webSocketManager.SendToConnectionAsync(
                        connectionId,
                        new { type = "pong" }
                    );
                    break;
                case "video_call_declined": // NEW: Handle video call declined
                    await HandleVideoCallDeclined(userId, messageData.Data);
                    break;
                default:
                    _logger.LogWarning("Unknown message type: {Type}", messageData.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket message");
            await _webSocketManager.SendToConnectionAsync(
                connectionId,
                new { type = "error", data = new { message = "Failed to process message" } }
            );
        }
    }

    private async Task HandleJoinRoom(
        IMediator mediator,
        Guid userId,
        string connectionId,
        JsonElement data
    )
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var command = new JoinRoomCommand(roomId, userId, connectionId);
        var result = await mediator.Send(command);

        if (result.Success)
        {
            await _webSocketManager.SendToRoomAsync(
                roomId,
                new { type = "user_joined", data = new { userId, roomId } },
                userId
            );
            await _webSocketManager.SendToConnectionAsync(
                connectionId,
                new { type = "joined_room", data = new { roomId, role = result.Role?.ToString() } }
            );
        }
        else
        {
            await _webSocketManager.SendToConnectionAsync(
                connectionId,
                new { type = "error", data = new { message = result.ErrorMessage } }
            );
        }
    }

    private async Task HandleLeaveRoom(IMediator mediator, Guid userId, JsonElement data)
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var command = new LeaveRoomCommand(roomId, userId);
        var result = await mediator.Send(command);

        if (result.Success)
        {
            await _webSocketManager.SendToRoomAsync(
                roomId,
                new { type = "user_left", data = new { userId, roomId } }
            );
        }
    }

    private async Task HandleSendMessage(IMediator mediator, Guid userId, JsonElement data)
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var content = data.GetProperty("content").GetString() ?? string.Empty;
        var messageType = Enum.Parse<MessageType>(
            data.GetProperty("messageType").GetString() ?? "Text"
        );

        var command = new SendMessageCommand(roomId, userId, content, messageType);
        await mediator.Send(command);
    }

    private async Task HandleTyping(Guid userId, JsonElement data)
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var isTyping = data.GetProperty("isTyping").GetBoolean();

        await _webSocketManager.SendToRoomAsync(
            roomId,
            new
            {
                type = "typing",
                data = new
                {
                    userId,
                    roomId,
                    isTyping,
                },
            },
            userId
        );
    }

    private async Task HandleVideoSignal(Guid userId, JsonElement data)
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var targetUserId = data.GetProperty("targetUserId").GetGuid();
        var signal = data.GetProperty("signal");

        _logger.LogInformation(
            "Handling video signal from {FromUserId} to {ToUserId} in room {RoomId}",
            userId,
            targetUserId,
            roomId
        );

        await _webSocketManager.SendToUserAsync(
            targetUserId,
            new
            {
                type = "video_signal",
                data = new
                {
                    fromUserId = userId,
                    roomId,
                    signal,
                },
            }
        );
    }

    private async Task HandleIceCandidate(Guid userId, JsonElement data)
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var targetUserId = data.GetProperty("targetUserId").GetGuid();
        var candidate = data.GetProperty("candidate");

        _logger.LogInformation(
            "Handling ICE candidate from {FromUserId} to {ToUserId} in room {RoomId}",
            userId,
            targetUserId,
            roomId
        );

        await _webSocketManager.SendToUserAsync(
            targetUserId,
            new
            {
                type = "ice_candidate",
                data = new
                {
                    fromUserId = userId,
                    roomId,
                    candidate,
                },
            }
        );
    }

    // NEW: Handle video call declined message
    private async Task HandleVideoCallDeclined(Guid userId, JsonElement data)
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var targetUserId = data.GetProperty("targetUserId").GetGuid();
        var callId = data.GetProperty("callId").GetGuid();

        _logger.LogInformation(
            "Video call {CallId} in room {RoomId} declined by user {UserId} for target {TargetUserId}",
            callId,
            roomId,
            userId,
            targetUserId
        );

        // Notify the initiator that the call was declined
        await _webSocketManager.SendToUserAsync(
            targetUserId, // This is the initiator
            new
            {
                type = "video_call_declined",
                data = new
                {
                    roomId,
                    callId,
                    declinedByUserId = userId, // Who declined
                },
            }
        );
    }
}

public class WebSocketMessage
{
    public string Type { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
}
