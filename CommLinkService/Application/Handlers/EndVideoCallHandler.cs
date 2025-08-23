using CommLinkService.Application.Commands;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.VideoCallDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class EndVideoCallHandler
    : IRequestHandler<EndVideoCallCommand, ApiResponse<VideoCallEndDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<EndVideoCallHandler> _logger;

    public EndVideoCallHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        ILogger<EndVideoCallHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ApiResponse<VideoCallEndDTO>> Handle(
        EndVideoCallCommand request,
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
                return new ApiResponse<VideoCallEndDTO>(false, "Room not found");

            // Crear mensaje de sistema para fin de llamada
            var endedAt = DateTime.UtcNow;
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(
                new { callId = request.CallId }
            );

            var message = new Message
            {
                Id = Guid.NewGuid(),
                RoomId = request.RoomId,
                SenderType = request.EndedByType,
                SenderTaxUserId = request.EndedByTaxUserId,
                SenderCustomerId = request.EndedByCustomerId,
                SenderCompanyId = room.CreatedByCompanyId,
                Content = "Video call ended",
                Type = MessageType.VideoCallEnd,
                Metadata = metadataJson,
                SentAt = endedAt,
                IsDeleted = false,
                CreatedAt = endedAt,
            };

            _context.Messages.Add(message);

            // Actualizar room activity
            room.LastActivityAt = endedAt;
            room.UpdatedAt = endedAt;

            await _context.SaveChangesAsync(cancellationToken);

            // Crear DTO de respuesta
            var endCallDto = new VideoCallEndDTO
            {
                CallId = request.CallId,
                EndedAt = endedAt,
                Success = true,
            };

            // Notificar por WebSocket
            await _webSocketManager.SendToRoomAsync(
                request.RoomId,
                new
                {
                    type = "video_call_end",
                    data = new
                    {
                        callId = request.CallId,
                        roomId = request.RoomId,
                        endedByType = request.EndedByType,
                        endedByTaxUserId = request.EndedByTaxUserId,
                        endedByCustomerId = request.EndedByCustomerId,
                        endedAt,
                    },
                }
            );

            _logger.LogInformation(
                "Video call {CallId} ended in room {RoomId} by {EndedByType} {EndedById}",
                request.CallId,
                request.RoomId,
                request.EndedByType,
                request.EndedByType == ParticipantType.TaxUser
                    ? request.EndedByTaxUserId
                    : request.EndedByCustomerId
            );

            return new ApiResponse<VideoCallEndDTO>(
                true,
                "Video call ended successfully",
                endCallDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending video call");
            return new ApiResponse<VideoCallEndDTO>(false, "Failed to end video call");
        }
    }
}
