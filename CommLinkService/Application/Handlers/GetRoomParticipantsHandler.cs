using AutoMapper;
using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.RoomDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetRoomParticipantsHandler
    : IRequestHandler<GetRoomParticipantsQuery, ApiResponse<List<RoomParticipantDTO>>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<GetRoomParticipantsHandler> _logger;

    public GetRoomParticipantsHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        ILogger<GetRoomParticipantsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ApiResponse<List<RoomParticipantDTO>>> Handle(
        GetRoomParticipantsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el requester es participante del room
            bool isParticipant = false;
            if (request.RequesterType == ParticipantType.TaxUser)
            {
                isParticipant = await _context.RoomParticipants.AnyAsync(
                    p =>
                        p.RoomId == request.RoomId
                        && p.ParticipantType == ParticipantType.TaxUser
                        && p.TaxUserId == request.RequesterTaxUserId
                        && p.IsActive,
                    cancellationToken
                );
            }
            else if (request.RequesterType == ParticipantType.Customer)
            {
                isParticipant = await _context.RoomParticipants.AnyAsync(
                    p =>
                        p.RoomId == request.RoomId
                        && p.ParticipantType == ParticipantType.Customer
                        && p.CustomerId == request.RequesterCustomerId
                        && p.IsActive,
                    cancellationToken
                );
            }

            if (!isParticipant)
                return new ApiResponse<List<RoomParticipantDTO>>(
                    false,
                    "You are not a participant in this room"
                );

            // Obtener participantes del room
            var participants = await _context
                .RoomParticipants.Where(p => p.RoomId == request.RoomId && p.IsActive)
                .ToListAsync(cancellationToken);

            // Mapear a DTOs y agregar info de estado online
            var participantDtos = participants
                .Select(p =>
                {
                    var dto = _mapper.Map<RoomParticipantDTO>(p);

                    // Verificar si está online según su tipo
                    if (p.ParticipantType == ParticipantType.TaxUser && p.TaxUserId.HasValue)
                    {
                        dto.IsOnline = _webSocketManager.IsTaxUserOnline(p.TaxUserId.Value);
                    }
                    else if (p.ParticipantType == ParticipantType.Customer && p.CustomerId.HasValue)
                    {
                        dto.IsOnline = _webSocketManager.IsCustomerOnline(p.CustomerId.Value);
                    }

                    return dto;
                })
                .ToList();

            return new ApiResponse<List<RoomParticipantDTO>>(
                true,
                "Participants retrieved successfully",
                participantDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting room participants for room {RoomId}",
                request.RoomId
            );
            return new ApiResponse<List<RoomParticipantDTO>>(
                false,
                "Failed to get room participants"
            );
        }
    }
}
