using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Application.Events;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.VideoCallDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace CommLinkService.Application.Handlers;

public sealed class StartVideoCallHandler
    : IRequestHandler<StartVideoCallCommand, ApiResponse<VideoCallDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<StartVideoCallHandler> _logger;

    public StartVideoCallHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        IEventBus eventBus,
        ILogger<StartVideoCallHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ApiResponse<VideoCallDTO>> Handle(
        StartVideoCallCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el room existe
            var room = await _context
                .Rooms.Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

            if (room == null)
                return new ApiResponse<VideoCallDTO>(false, "Room not found");

            // Verificar que el iniciador es participante activo
            bool isParticipant = false;
            if (request.InitiatorType == ParticipantType.TaxUser)
            {
                isParticipant = room.Participants.Any(p =>
                    p.ParticipantType == ParticipantType.TaxUser
                    && p.TaxUserId == request.InitiatorTaxUserId
                    && p.IsActive
                );
            }
            else if (request.InitiatorType == ParticipantType.Customer)
            {
                isParticipant = room.Participants.Any(p =>
                    p.ParticipantType == ParticipantType.Customer
                    && p.CustomerId == request.InitiatorCustomerId
                    && p.IsActive
                );
            }

            if (!isParticipant)
                return new ApiResponse<VideoCallDTO>(
                    false,
                    "User is not a participant in this room"
                );

            // Crear callId y mensaje de sistema
            var callId = Guid.NewGuid();
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(new { callId });

            var message = new Message
            {
                Id = Guid.NewGuid(),
                RoomId = request.RoomId,
                SenderType = request.InitiatorType,
                SenderTaxUserId = request.InitiatorTaxUserId,
                SenderCustomerId = request.InitiatorCustomerId,
                SenderCompanyId = request.InitiatorCompanyId,
                Content = "Video call started",
                Type = MessageType.VideoCallStart,
                Metadata = metadataJson,
                SentAt = DateTime.UtcNow,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Messages.Add(message);

            // Actualizar room activity
            room.LastActivityAt = DateTime.UtcNow;
            room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Crear DTO de respuesta
            var videoCallDto = new VideoCallDTO
            {
                CallId = callId,
                RoomId = request.RoomId,
                RoomName = room.Name,
                StartedAt = DateTime.UtcNow,
                SignalServer = "ws://localhost/ws/",
                IceServers = new Dictionary<string, object>
                {
                    ["iceServers"] = new[]
                    {
                        new { urls = "stun:stun.l.google.com:19302" },
                        new { urls = "stun:stun1.l.google.com:19302" },
                    },
                },
                Participants = room
                    .Participants.Where(p => p.IsActive)
                    .Select(p => new VideoCallParticipantDTO
                    {
                        ParticipantType = p.ParticipantType,
                        TaxUserId = p.TaxUserId,
                        CustomerId = p.CustomerId,
                        DisplayName = "", // Frontend resuelve
                        IsMuted = p.IsMuted,
                        IsVideoEnabled = p.IsVideoEnabled,
                    })
                    .ToList(),
            };

            // Notificar por WebSocket a otros participantes
            var wsPayload = new
            {
                type = "video_call_start",
                data = new
                {
                    callId,
                    roomId = request.RoomId,
                    initiatorType = request.InitiatorType,
                    initiatorTaxUserId = request.InitiatorTaxUserId,
                    initiatorCustomerId = request.InitiatorCustomerId,
                },
            };

            await _webSocketManager.SendToRoomAsync(
                request.RoomId,
                wsPayload,
                request.InitiatorType,
                request.InitiatorType == ParticipantType.TaxUser
                    ? request.InitiatorTaxUserId
                    : request.InitiatorCustomerId
            );

            // Publicar evento
            var initiatorId =
                request.InitiatorType == ParticipantType.TaxUser
                    ? request.InitiatorTaxUserId
                    : request.InitiatorCustomerId;

            _eventBus.Publish(
                new VideoCallStartedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    callId,
                    request.RoomId,
                    initiatorId!.Value,
                    room.Participants.Where(p => p.IsActive)
                        .Select(p =>
                            p.ParticipantType == ParticipantType.TaxUser
                                ? p.TaxUserId!.Value
                                : p.CustomerId!.Value
                        )
                        .ToList()
                )
            );

            _logger.LogInformation(
                "Video call {CallId} started in room {RoomId} by {InitiatorType} {InitiatorId}",
                callId,
                request.RoomId,
                request.InitiatorType,
                initiatorId
            );

            return new ApiResponse<VideoCallDTO>(
                true,
                "Video call started successfully",
                videoCallDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting video call");
            return new ApiResponse<VideoCallDTO>(false, "Failed to start video call");
        }
    }
}
