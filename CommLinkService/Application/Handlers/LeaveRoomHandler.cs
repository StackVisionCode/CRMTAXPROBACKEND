using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class LeaveRoomHandler : IRequestHandler<LeaveRoomCommand, ApiResponse<bool>>
{
    private readonly ICommLinkDbContext _context;
    private readonly ILogger<LeaveRoomHandler> _logger;

    public LeaveRoomHandler(ICommLinkDbContext context, ILogger<LeaveRoomHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        LeaveRoomCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Buscar participante activo
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
                return new ApiResponse<bool>(false, "Participant not found or not active in room");

            // Marcar como inactivo
            participant.IsActive = false;
            participant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "{ParticipantType} {UserId} left room {RoomId}",
                request.ParticipantType,
                request.ParticipantType == ParticipantType.TaxUser
                    ? request.TaxUserId
                    : request.CustomerId,
                request.RoomId
            );

            return new ApiResponse<bool>(true, "Left room successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving room");
            return new ApiResponse<bool>(false, "Failed to leave room");
        }
    }
}
