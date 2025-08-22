using AutoMapper;
using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.MessageDTOs;
using DTOs.RoomDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetRoomByIdHandler : IRequestHandler<GetRoomByIdQuery, ApiResponse<RoomDTO?>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRoomByIdHandler> _logger;

    public GetRoomByIdHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        ILogger<GetRoomByIdHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<RoomDTO?>> Handle(
        GetRoomByIdQuery request,
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
                return new ApiResponse<RoomDTO?>(false, "You are not a participant in this room");

            // Obtener room con participantes
            var room = await _context
                .Rooms.Include(r => r.Participants)
                .Include(r => r.Messages.Where(m => !m.IsDeleted))
                .ThenInclude(m => m.Reactions)
                .FirstOrDefaultAsync(r => r.Id == request.RoomId && r.IsActive, cancellationToken);

            if (room == null)
                return new ApiResponse<RoomDTO?>(true, "Room not found", null);

            // Mapear a DTO
            var roomDto = _mapper.Map<RoomDTO>(room);

            // Último mensaje
            var lastMessage = room.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

            if (lastMessage != null)
            {
                roomDto.LastMessage = _mapper.Map<MessageDTO>(lastMessage);
            }

            return new ApiResponse<RoomDTO?>(true, "Room retrieved successfully", roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room {RoomId}", request.RoomId);
            return new ApiResponse<RoomDTO?>(false, "Failed to get room");
        }
    }
}
