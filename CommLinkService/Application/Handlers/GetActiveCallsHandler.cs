using System.Text.Json;
using CommLinkService.Application.Queries;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetActiveCallsHandler
    : IRequestHandler<GetActiveCallsQuery, GetActiveCallsResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly ILogger<GetActiveCallsHandler> _logger;

    public GetActiveCallsHandler(ICommLinkDbContext context, ILogger<GetActiveCallsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetActiveCallsResult> Handle(
        GetActiveCallsQuery request,
        CancellationToken cancellationToken
    )
    {
        // Traemos las salas donde el usuario participa (activo)
        var userRoomIds = await _context
            .RoomParticipants.Where(p => p.UserId == request.UserId && p.IsActive)
            .Select(p => p.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userRoomIds.Count == 0)
            return new GetActiveCallsResult(new());

        // Para cada sala, obtenemos el último VideoCallStart y
        // verificamos si después hubo un VideoCallEnd.
        var activeCalls = new List<ActiveCallDto>();

        // Cargar mensajes relevantes en batch
        var messages = await _context
            .Messages.Where(m =>
                userRoomIds.Contains(m.RoomId)
                && (m.Type == MessageType.VideoCallStart || m.Type == MessageType.VideoCallEnd)
            )
            .OrderBy(m => m.SentAt) // asumiendo propiedad SentAtUtc; ajusta si es diferente
            .ToListAsync(cancellationToken);

        // Agrupar por sala
        var byRoom = messages.GroupBy(m => m.RoomId);

        foreach (var group in byRoom)
        {
            // último start
            var lastStart = group.LastOrDefault(m => m.Type == MessageType.VideoCallStart);
            if (lastStart is null)
                continue;

            // ¿hay end posterior al start?
            var anyEndAfter = group.Any(m =>
                m.Type == MessageType.VideoCallEnd
                && m.SentAt > lastStart.SentAt
                && TryExtractCallId(m.Metadata, out var endCallId)
                && TryExtractCallId(lastStart.Metadata, out var startCallId)
                && endCallId == startCallId
            );

            if (anyEndAfter)
                continue; // no está activa

            // Extraer callId
            if (!TryExtractCallId(lastStart.Metadata, out var callId))
                continue;

            // Participantes (activos en sala)
            var roomParticipants = await _context
                .RoomParticipants.Where(p => p.RoomId == group.Key && p.IsActive)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

            // Room info
            var roomInfo = await _context
                .Rooms.Where(r => r.Id == group.Key)
                .Select(r => new { r.Id, r.Name })
                .FirstAsync(cancellationToken);

            activeCalls.Add(
                new ActiveCallDto(
                    callId,
                    roomInfo.Id,
                    roomInfo.Name,
                    lastStart.SentAt, // o CreatedAt si corresponde
                    roomParticipants
                )
            );
        }

        return new GetActiveCallsResult(activeCalls);
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
