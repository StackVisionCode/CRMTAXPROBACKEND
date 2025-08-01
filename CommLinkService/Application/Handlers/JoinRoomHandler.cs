using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class JoinRoomHandler : IRequestHandler<JoinRoomCommand, JoinRoomResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly ILogger<JoinRoomHandler> _logger;

    public JoinRoomHandler(ICommLinkDbContext context, ILogger<JoinRoomHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<JoinRoomResult> Handle(
        JoinRoomCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // First check if room exists and has capacity
            var room = await _context
                .Rooms.Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == request.RoomId && r.IsActive, cancellationToken);

            if (room == null)
                return new JoinRoomResult(false, "Room not found", null);

            // Check capacity first to avoid unnecessary queries
            var activeParticipants = room.Participants.Count(p => p.IsActive);
            if (activeParticipants >= room.MaxParticipants)
                return new JoinRoomResult(false, "Room is full", null);

            // Try to find existing participant
            var existingParticipant = room.Participants.FirstOrDefault(p =>
                p.UserId == request.UserId
            );

            if (existingParticipant != null)
            {
                if (existingParticipant.IsActive)
                    return new JoinRoomResult(true, null, existingParticipant.Role);

                // Use direct update approach
                var rowsAffected = await _context
                    .RoomParticipants.Where(p => p.Id == existingParticipant.Id && !p.IsActive)
                    .ExecuteUpdateAsync(setters =>
                        setters
                            .SetProperty(p => p.IsActive, true)
                            .SetProperty(p => p.JoinedAt, DateTime.UtcNow)
                    );

                if (rowsAffected == 0)
                    return new JoinRoomResult(false, "Failed to reactivate participant", null);

                _logger.LogInformation(
                    "User {UserId} rejoined room {RoomId}",
                    request.UserId,
                    request.RoomId
                );
                return new JoinRoomResult(true, null, existingParticipant.Role);
            }

            // Add new participant
            var newParticipant = new RoomParticipant(
                room.Id,
                request.UserId,
                ParticipantRole.Member
            );
            _context.RoomParticipants.Add(newParticipant);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "User {UserId} joined room {RoomId}",
                    request.UserId,
                    request.RoomId
                );
                return new JoinRoomResult(true, null, ParticipantRole.Member);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Handle race condition where participant was added by another request
                return await Handle(request, cancellationToken); // Retry
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error joining room {RoomId} for user {UserId}",
                request.RoomId,
                request.UserId
            );
            return new JoinRoomResult(false, "An error occurred while joining the room", null);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627); // Unique constraint violation codes
    }
}
