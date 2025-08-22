using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetUnreadMessageCountHandler
    : IRequestHandler<GetUnreadMessageCountQuery, ApiResponse<int>>
{
    private readonly ICommLinkDbContext _context;
    private readonly ILogger<GetUnreadMessageCountHandler> _logger;

    public GetUnreadMessageCountHandler(
        ICommLinkDbContext context,
        ILogger<GetUnreadMessageCountHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<int>> Handle(
        GetUnreadMessageCountQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el usuario es participante del room
            bool isParticipant = false;
            if (request.UserType == ParticipantType.TaxUser)
            {
                isParticipant = await _context.RoomParticipants.AnyAsync(
                    p =>
                        p.RoomId == request.RoomId
                        && p.ParticipantType == ParticipantType.TaxUser
                        && p.TaxUserId == request.TaxUserId
                        && p.IsActive,
                    cancellationToken
                );
            }
            else if (request.UserType == ParticipantType.Customer)
            {
                isParticipant = await _context.RoomParticipants.AnyAsync(
                    p =>
                        p.RoomId == request.RoomId
                        && p.ParticipantType == ParticipantType.Customer
                        && p.CustomerId == request.CustomerId
                        && p.IsActive,
                    cancellationToken
                );
            }

            if (!isParticipant)
                return new ApiResponse<int>(false, "You are not a participant in this room");

            // Contar mensajes no leídos (simplificado - en producción usar tabla de lecturas)
            // Por ahora contamos mensajes de otros usuarios en los últimos 7 días
            var unreadCount = await _context
                .Messages.Where(m =>
                    m.RoomId == request.RoomId
                    && !m.IsDeleted
                    && m.SentAt > DateTime.UtcNow.AddDays(-7)
                )
                .Where(m =>
                    (
                        request.UserType == ParticipantType.TaxUser
                        && (
                            m.SenderType != ParticipantType.TaxUser
                            || m.SenderTaxUserId != request.TaxUserId
                        )
                    )
                    || (
                        request.UserType == ParticipantType.Customer
                        && (
                            m.SenderType != ParticipantType.Customer
                            || m.SenderCustomerId != request.CustomerId
                        )
                    )
                )
                .CountAsync(cancellationToken);

            return new ApiResponse<int>(true, "Unread count retrieved successfully", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for room {RoomId}", request.RoomId);
            return new ApiResponse<int>(false, "Failed to get unread count");
        }
    }
}
