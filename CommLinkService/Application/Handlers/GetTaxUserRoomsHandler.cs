using AutoMapper;
using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.MessageDTOs;
using DTOs.RoomDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetTaxUserRoomsHandler
    : IRequestHandler<GetTaxUserRoomsQuery, ApiResponse<List<RoomDTO>>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<GetTaxUserRoomsHandler> _logger;

    public GetTaxUserRoomsHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        ILogger<GetTaxUserRoomsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ApiResponse<List<RoomDTO>>> Handle(
        GetTaxUserRoomsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Obtener rooms donde el TaxUser participa
            var userRooms = await _context
                .RoomParticipants.Include(p => p.Room)
                .ThenInclude(r => r.Participants)
                .Include(p => p.Room)
                .ThenInclude(r => r.Messages)
                .Where(p =>
                    p.ParticipantType == ParticipantType.TaxUser
                    && p.TaxUserId == request.TaxUserId
                    && p.CompanyId == request.CompanyId
                    && p.IsActive
                    && p.Room.IsActive
                )
                .Select(p => p.Room)
                .Distinct()
                .ToListAsync(cancellationToken);

            var roomDtos = new List<RoomDTO>();

            foreach (var room in userRooms)
            {
                // Mapear a DTO básico
                var roomDto = _mapper.Map<RoomDTO>(room);

                // Calcular mensajes no leídos (simplificado)
                roomDto.UnreadCount = room.Messages.Count(m =>
                    m.SenderType != ParticipantType.TaxUser
                    || m.SenderTaxUserId != request.TaxUserId
                        && m.SentAt > DateTime.UtcNow.AddDays(-7)
                );

                // Mapear participantes con estado online
                roomDto.Participants = room
                    .Participants.Where(p => p.IsActive)
                    .Select(p =>
                    {
                        var participantDto = _mapper.Map<RoomParticipantDTO>(p);

                        if (p.ParticipantType == ParticipantType.TaxUser && p.TaxUserId.HasValue)
                        {
                            participantDto.IsOnline = _webSocketManager.IsTaxUserOnline(
                                p.TaxUserId.Value
                            );
                        }
                        else if (
                            p.ParticipantType == ParticipantType.Customer
                            && p.CustomerId.HasValue
                        )
                        {
                            participantDto.IsOnline = _webSocketManager.IsCustomerOnline(
                                p.CustomerId.Value
                            );
                        }

                        return participantDto;
                    })
                    .ToList();

                // Último mensaje
                var lastMessage = room
                    .Messages.Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                if (lastMessage != null)
                {
                    roomDto.LastMessage = _mapper.Map<MessageDTO>(lastMessage);
                }

                roomDtos.Add(roomDto);
            }

            // Ordenar por actividad más reciente
            var sortedRooms = roomDtos.OrderByDescending(r => r.LastActivityAt).ToList();

            return new ApiResponse<List<RoomDTO>>(
                true,
                "Rooms retrieved successfully",
                sortedRooms
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rooms for TaxUser {TaxUserId}", request.TaxUserId);
            return new ApiResponse<List<RoomDTO>>(false, "Failed to get rooms");
        }
    }
}
