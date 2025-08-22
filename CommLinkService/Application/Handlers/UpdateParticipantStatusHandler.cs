using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.RoomDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class UpdateParticipantStatusHandler
    : IRequestHandler<UpdateParticipantStatusCommand, ApiResponse<RoomParticipantDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<UpdateParticipantStatusHandler> _logger;

    public UpdateParticipantStatusHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        ILogger<UpdateParticipantStatusHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ApiResponse<RoomParticipantDTO>> Handle(
        UpdateParticipantStatusCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Buscar participante
            var participant = await _context.RoomParticipants.FirstOrDefaultAsync(
                p =>
                    p.RoomId == request.RoomId
                    && p.IsActive
                    && (
                        (
                            request.ParticipantType == ParticipantType.TaxUser
                            && p.TaxUserId == request.TaxUserId
                        )
                        || (
                            request.ParticipantType == ParticipantType.Customer
                            && p.CustomerId == request.CustomerId
                        )
                    ),
                cancellationToken
            );

            if (participant == null)
                return new ApiResponse<RoomParticipantDTO>(false, "Participant not found");

            // Actualizar status
            if (request.IsMuted.HasValue)
                participant.IsMuted = request.IsMuted.Value;

            if (request.IsVideoEnabled.HasValue)
                participant.IsVideoEnabled = request.IsVideoEnabled.Value;

            participant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Mapear a DTO
            var participantDto = _mapper.Map<RoomParticipantDTO>(participant);

            // Notificar por WebSocket
            await _webSocketManager.SendToRoomAsync(
                request.RoomId,
                new
                {
                    type = "participant_status_updated",
                    data = new
                    {
                        participantType = request.ParticipantType,
                        taxUserId = request.TaxUserId,
                        customerId = request.CustomerId,
                        isMuted = participant.IsMuted,
                        isVideoEnabled = participant.IsVideoEnabled,
                    },
                },
                request.ParticipantType,
                request.ParticipantType == ParticipantType.TaxUser
                    ? request.TaxUserId
                    : request.CustomerId
            );

            _logger.LogInformation(
                "Participant {ParticipantType} {UserId} status updated in room {RoomId}",
                request.ParticipantType,
                request.ParticipantType == ParticipantType.TaxUser
                    ? request.TaxUserId
                    : request.CustomerId,
                request.RoomId
            );

            return new ApiResponse<RoomParticipantDTO>(
                true,
                "Participant status updated successfully",
                participantDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating participant status");
            return new ApiResponse<RoomParticipantDTO>(
                false,
                "Failed to update participant status"
            );
        }
    }
}
