using System.Text.Json;
using CommLinkService.Application.Queries;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.VideoCallDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetActiveCallsHandler
    : IRequestHandler<GetActiveVideoCallsQuery, ApiResponse<List<ActiveVideoCallDTO>>>
{
    private readonly ICommLinkDbContext _context;
    private readonly ILogger<GetActiveCallsHandler> _logger;

    public GetActiveCallsHandler(ICommLinkDbContext context, ILogger<GetActiveCallsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ActiveVideoCallDTO>>> Handle(
        GetActiveVideoCallsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Obtener rooms donde el usuario participa (según su tipo)
            var userRoomIds = new List<Guid>();

            if (request.UserType == ParticipantType.TaxUser && request.TaxUserId.HasValue)
            {
                userRoomIds = await _context
                    .RoomParticipants.Where(p =>
                        p.ParticipantType == ParticipantType.TaxUser
                        && p.TaxUserId == request.TaxUserId
                        && p.IsActive
                    )
                    .Select(p => p.RoomId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            else if (request.UserType == ParticipantType.Customer && request.CustomerId.HasValue)
            {
                userRoomIds = await _context
                    .RoomParticipants.Where(p =>
                        p.ParticipantType == ParticipantType.Customer
                        && p.CustomerId == request.CustomerId
                        && p.IsActive
                    )
                    .Select(p => p.RoomId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }

            if (userRoomIds.Count == 0)
                return new ApiResponse<List<ActiveVideoCallDTO>>(
                    true,
                    "No rooms found",
                    new List<ActiveVideoCallDTO>()
                );

            // Cargar mensajes de video calls
            var messages = await _context
                .Messages.Where(m =>
                    userRoomIds.Contains(m.RoomId)
                    && (m.Type == MessageType.VideoCallStart || m.Type == MessageType.VideoCallEnd)
                )
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);

            var activeCalls = new List<ActiveVideoCallDTO>();

            // Agrupar por sala y verificar calls activos
            var byRoom = messages.GroupBy(m => m.RoomId);

            foreach (var group in byRoom)
            {
                var lastStart = group.LastOrDefault(m => m.Type == MessageType.VideoCallStart);
                if (lastStart == null)
                    continue;

                // Verificar si hay end posterior al start
                var anyEndAfter = group.Any(m =>
                    m.Type == MessageType.VideoCallEnd
                    && m.SentAt > lastStart.SentAt
                    && TryExtractCallId(m.Metadata, out var endCallId)
                    && TryExtractCallId(lastStart.Metadata, out var startCallId)
                    && endCallId == startCallId
                );

                if (anyEndAfter)
                    continue; // Call ya terminó

                if (!TryExtractCallId(lastStart.Metadata, out var callId))
                    continue;

                // Obtener participantes activos
                var roomParticipants = await _context
                    .RoomParticipants.Where(p => p.RoomId == group.Key && p.IsActive)
                    .Select(p =>
                        p.ParticipantType == ParticipantType.TaxUser
                            ? p.TaxUserId!.Value
                            : p.CustomerId!.Value
                    )
                    .ToListAsync(cancellationToken);

                // Obtener info del room
                var roomInfo = await _context
                    .Rooms.Where(r => r.Id == group.Key)
                    .Select(r => new { r.Id, r.Name })
                    .FirstAsync(cancellationToken);

                activeCalls.Add(
                    new ActiveVideoCallDTO
                    {
                        CallId = callId,
                        RoomId = roomInfo.Id,
                        RoomName = roomInfo.Name,
                        StartedAt = lastStart.SentAt,
                        ParticipantIds = roomParticipants,
                    }
                );
            }

            return new ApiResponse<List<ActiveVideoCallDTO>>(
                true,
                "Active calls retrieved successfully",
                activeCalls
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active calls");
            return new ApiResponse<List<ActiveVideoCallDTO>>(false, "Failed to get active calls");
        }
    }

    private static bool TryExtractCallId(string? json, out Guid callId)
    {
        callId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("callId", out var el) && el.TryGetGuid(out var id))
            {
                callId = id;
                return true;
            }
        }
        catch
        { /* ignore */
        }
        return false;
    }
}
