using System.Text.Json;
using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Services;
using DTOs.MessageDTOs;
using DTOs.RoomDTOs;
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

    // Actualizar signature para manejar ParticipantType
    public async Task HandleMessageAsync(
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        Guid? companyId,
        string connectionId,
        string message
    )
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
                    await HandleJoinRoom(
                        mediator,
                        userType,
                        taxUserId,
                        customerId,
                        companyId,
                        connectionId,
                        messageData.Data
                    );
                    break;
                case "leave_room":
                    await HandleLeaveRoom(
                        mediator,
                        userType,
                        taxUserId,
                        customerId,
                        messageData.Data
                    );
                    break;
                case "send_message":
                    await HandleSendMessage(
                        mediator,
                        userType,
                        taxUserId,
                        customerId,
                        companyId,
                        messageData.Data
                    );
                    break;
                case "typing":
                    await HandleTyping(userType, taxUserId, customerId, messageData.Data);
                    break;
                case "video_signal":
                    await HandleVideoSignal(userType, taxUserId, customerId, messageData.Data);
                    break;
                case "ice_candidate":
                    await HandleIceCandidate(userType, taxUserId, customerId, messageData.Data);
                    break;
                case "ping":
                    await _webSocketManager.SendToConnectionAsync(
                        connectionId,
                        new { type = "pong" }
                    );
                    break;
                case "video_call_declined":
                    await HandleVideoCallDeclined(
                        userType,
                        taxUserId,
                        customerId,
                        messageData.Data
                    );
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

    // Actualizar todos los handlers
    private async Task HandleJoinRoom(
        IMediator mediator,
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        Guid? companyId,
        string connectionId,
        JsonElement data
    )
    {
        var roomId = data.GetProperty("roomId").GetGuid();

        var command = new JoinRoomCommand(
            roomId,
            userType,
            taxUserId,
            customerId,
            companyId,
            connectionId
        );

        var result = await mediator.Send(command);

        if (result.Success.HasValue && result.Success.Value && result.Data != null)
        {
            await _webSocketManager.SendToRoomAsync(
                roomId,
                new
                {
                    type = "user_joined",
                    data = new
                    {
                        userType,
                        taxUserId,
                        customerId,
                        roomId,
                    },
                },
                userType,
                userType == ParticipantType.TaxUser ? taxUserId : customerId
            );

            await _webSocketManager.SendToConnectionAsync(
                connectionId,
                new
                {
                    type = "joined_room",
                    data = new { roomId, role = result.Data.Role.ToString() },
                }
            );
        }
        else
        {
            await _webSocketManager.SendToConnectionAsync(
                connectionId,
                new { type = "error", data = new { message = result.Message } }
            );
        }
    }

    private async Task HandleLeaveRoom(
        IMediator mediator,
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        JsonElement data
    )
    {
        var roomId = data.GetProperty("roomId").GetGuid();

        var command = new LeaveRoomCommand(roomId, userType, taxUserId, customerId);
        var result = await mediator.Send(command);

        if ((result.Success ?? false) && result.Data == true)
        {
            await _webSocketManager.SendToRoomAsync(
                roomId,
                new
                {
                    type = "user_left",
                    data = new
                    {
                        userType,
                        taxUserId,
                        customerId,
                        roomId,
                    },
                }
            );
        }
    }

    private async Task HandleSendMessage(
        IMediator mediator,
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        Guid? companyId,
        JsonElement data
    )
    {
        var roomId = data.GetProperty("roomId").GetGuid();
        var content = data.GetProperty("content").GetString() ?? string.Empty;
        var messageType = Enum.Parse<MessageType>(
            data.GetProperty("messageType").GetString() ?? "Text"
        );

        var sendMessageDto = new SendMessageDTO
        {
            RoomId = roomId,
            SenderType = userType,
            SenderTaxUserId = taxUserId,
            SenderCustomerId = customerId,
            SenderCompanyId = companyId,
            Content = content,
            Type = messageType,
        };

        var command = new SendMessageCommand(sendMessageDto);
        var result = await mediator.Send(command);

        if ((result.Success ?? false) && result.Data != null)
        {
            // Enviar mensaje a todos los participantes del room
            await _webSocketManager.SendToRoomAsync(
                roomId,
                new { type = "new_message", data = result.Data }
            );
        }
    }

    private async Task HandleTyping(
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        JsonElement data
    )
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
                    userType,
                    taxUserId,
                    customerId,
                    roomId,
                    isTyping,
                },
            },
            userType,
            userType == ParticipantType.TaxUser ? taxUserId : customerId
        );
    }

    private async Task HandleVideoSignal(
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        JsonElement data
    )
    {
        try
        {
            var roomId = data.GetProperty("roomId").GetGuid();

            // Manejar tanto string como number para ParticipantType
            ParticipantType targetUserType;
            if (data.TryGetProperty("targetUserType", out var targetTypeElement))
            {
                if (targetTypeElement.ValueKind == JsonValueKind.String)
                {
                    targetUserType = Enum.Parse<ParticipantType>(
                        targetTypeElement.GetString() ?? "TaxUser"
                    );
                }
                else if (targetTypeElement.ValueKind == JsonValueKind.Number)
                {
                    targetUserType = (ParticipantType)targetTypeElement.GetInt32();
                }
                else
                {
                    targetUserType = ParticipantType.TaxUser; // default
                }
            }
            else
            {
                targetUserType = ParticipantType.TaxUser; // default
            }

            // Manejar GUIDs que pueden venir como string o null
            Guid? targetTaxUserId = null;
            Guid? targetCustomerId = null;

            if (
                data.TryGetProperty("targetTaxUserId", out var taxUserElement)
                && taxUserElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(taxUserElement.GetString(), out var parsedTaxUserId)
            )
            {
                targetTaxUserId = parsedTaxUserId;
            }

            if (
                data.TryGetProperty("targetCustomerId", out var customerElement)
                && customerElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(customerElement.GetString(), out var parsedCustomerId)
            )
            {
                targetCustomerId = parsedCustomerId;
            }

            var signal = data.GetProperty("signal");

            var responseData = new
            {
                type = "video_signal",
                data = new
                {
                    fromUserType = userType,
                    fromTaxUserId = taxUserId,
                    fromCustomerId = customerId,
                    roomId,
                    signal,
                },
            };

            if (targetUserType == ParticipantType.TaxUser && targetTaxUserId.HasValue)
            {
                await _webSocketManager.SendToTaxUserAsync(targetTaxUserId.Value, responseData);
            }
            else if (targetUserType == ParticipantType.Customer && targetCustomerId.HasValue)
            {
                await _webSocketManager.SendToCustomerAsync(targetCustomerId.Value, responseData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling video signal: {Data}", data.ToString());
        }
    }

    private async Task HandleIceCandidate(
        ParticipantType userType,
        Nullable<Guid> taxUserId,
        Nullable<Guid> customerId,
        JsonElement data
    )
    {
        try
        {
            var roomId = data.GetProperty("roomId").GetGuid();

            // CORECCIÓN: Manejar tanto string como number para ParticipantType
            ParticipantType targetUserType;
            if (data.TryGetProperty("targetUserType", out var targetTypeElement))
            {
                if (targetTypeElement.ValueKind == JsonValueKind.String)
                {
                    targetUserType = Enum.Parse<ParticipantType>(
                        targetTypeElement.GetString() ?? "TaxUser"
                    );
                }
                else if (targetTypeElement.ValueKind == JsonValueKind.Number)
                {
                    targetUserType = (ParticipantType)targetTypeElement.GetInt32();
                }
                else
                {
                    targetUserType = ParticipantType.TaxUser; // default
                }
            }
            else
            {
                targetUserType = ParticipantType.TaxUser; // default
            }

            // CORECCIÓN: Manejar GUIDs que pueden venir como string o null
            Guid? targetTaxUserId = null;
            Guid? targetCustomerId = null;

            if (
                data.TryGetProperty("targetTaxUserId", out var taxUserElement)
                && taxUserElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(taxUserElement.GetString(), out var parsedTaxUserId)
            )
            {
                targetTaxUserId = parsedTaxUserId;
            }

            if (
                data.TryGetProperty("targetCustomerId", out var customerElement)
                && customerElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(customerElement.GetString(), out var parsedCustomerId)
            )
            {
                targetCustomerId = parsedCustomerId;
            }

            var candidate = data.GetProperty("candidate");

            var responseData = new
            {
                type = "ice_candidate",
                data = new
                {
                    fromUserType = userType,
                    fromTaxUserId = taxUserId,
                    fromCustomerId = customerId,
                    roomId,
                    candidate,
                },
            };

            if (targetUserType == ParticipantType.TaxUser && targetTaxUserId.HasValue)
            {
                await _webSocketManager.SendToTaxUserAsync(targetTaxUserId.Value, responseData);
            }
            else if (targetUserType == ParticipantType.Customer && targetCustomerId.HasValue)
            {
                await _webSocketManager.SendToCustomerAsync(targetCustomerId.Value, responseData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ice candidate: {Data}", data.ToString());
        }
    }

    private async Task HandleVideoCallDeclined(
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        JsonElement data
    )
    {
        try
        {
            var roomId = data.GetProperty("roomId").GetGuid();
            var callId = data.GetProperty("callId").GetGuid();

            // Manejar tanto string como number para ParticipantType
            ParticipantType targetUserType;
            if (data.TryGetProperty("targetUserType", out var targetTypeElement))
            {
                if (targetTypeElement.ValueKind == JsonValueKind.String)
                {
                    targetUserType = Enum.Parse<ParticipantType>(
                        targetTypeElement.GetString() ?? "TaxUser"
                    );
                }
                else if (targetTypeElement.ValueKind == JsonValueKind.Number)
                {
                    targetUserType = (ParticipantType)targetTypeElement.GetInt32();
                }
                else
                {
                    targetUserType = ParticipantType.TaxUser; // default
                }
            }
            else
            {
                targetUserType = ParticipantType.TaxUser; // default
            }

            // Manejar GUIDs que pueden venir como string o null
            Guid? targetTaxUserId = null;
            Guid? targetCustomerId = null;

            if (
                data.TryGetProperty("targetTaxUserId", out var taxUserElement)
                && taxUserElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(taxUserElement.GetString(), out var parsedTaxUserId)
            )
            {
                targetTaxUserId = parsedTaxUserId;
            }

            if (
                data.TryGetProperty("targetCustomerId", out var customerElement)
                && customerElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(customerElement.GetString(), out var parsedCustomerId)
            )
            {
                targetCustomerId = parsedCustomerId;
            }

            var responseData = new
            {
                type = "video_call_declined",
                data = new
                {
                    roomId,
                    callId,
                    declinedByUserType = userType,
                    declinedByTaxUserId = taxUserId,
                    declinedByCustomerId = customerId,
                },
            };

            if (targetUserType == ParticipantType.TaxUser && targetTaxUserId.HasValue)
            {
                await _webSocketManager.SendToTaxUserAsync(targetTaxUserId.Value, responseData);
            }
            else if (targetUserType == ParticipantType.Customer && targetCustomerId.HasValue)
            {
                await _webSocketManager.SendToCustomerAsync(targetCustomerId.Value, responseData);
            }

            _logger.LogInformation(
                "Video call declined by {UserType} {UserId} in room {RoomId}",
                userType,
                userType == ParticipantType.TaxUser ? taxUserId : customerId,
                roomId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling video call declined: {Data}", data.ToString());
        }
    }
}

public class WebSocketMessage
{
    public string Type { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
}
