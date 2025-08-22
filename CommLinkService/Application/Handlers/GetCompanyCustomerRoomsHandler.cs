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

public sealed class GetCompanyCustomerRoomsHandler
    : IRequestHandler<GetCompanyCustomerRoomsQuery, ApiResponse<List<RoomDTO>>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<GetCompanyCustomerRoomsHandler> _logger;

    public GetCompanyCustomerRoomsHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        ILogger<GetCompanyCustomerRoomsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ApiResponse<List<RoomDTO>>> Handle(
        GetCompanyCustomerRoomsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Obtener rooms donde la Company tiene TaxUsers participando
            var query = _context
                .Rooms.Include(r => r.Participants)
                .Include(r => r.Messages)
                .Where(r =>
                    r.IsActive
                    && r.Participants.Any(p =>
                        p.ParticipantType == ParticipantType.TaxUser
                        && p.CompanyId == request.CompanyId
                        && p.IsActive
                    )
                );

            // Si se especifica CustomerId, filtrar por ese Customer
            if (request.CustomerId.HasValue)
            {
                query = query.Where(r =>
                    r.Participants.Any(p =>
                        p.ParticipantType == ParticipantType.Customer
                        && p.CustomerId == request.CustomerId.Value
                        && p.IsActive
                    )
                );
            }
            else
            {
                // Solo rooms que tengan al menos un Customer
                query = query.Where(r =>
                    r.Participants.Any(p =>
                        p.ParticipantType == ParticipantType.Customer && p.IsActive
                    )
                );
            }

            var rooms = await query
                .OrderByDescending(r => r.LastActivityAt ?? r.CreatedAt)
                .ToListAsync(cancellationToken);

            var roomDtos = new List<RoomDTO>();

            foreach (var room in rooms)
            {
                // Mapear a DTO básico
                var roomDto = _mapper.Map<RoomDTO>(room);

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

            return new ApiResponse<List<RoomDTO>>(
                true,
                "Company customer rooms retrieved successfully",
                roomDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting company customer rooms for Company {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<List<RoomDTO>>(false, "Failed to get company customer rooms");
        }
    }
}
