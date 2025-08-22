using AutoMapper;
using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.RoomDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetRoomBetweenCustomerAndTaxUserHandler
    : IRequestHandler<GetRoomBetweenCustomerAndTaxUserQuery, ApiResponse<RoomDTO?>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRoomBetweenCustomerAndTaxUserHandler> _logger;

    public GetRoomBetweenCustomerAndTaxUserHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        ILogger<GetRoomBetweenCustomerAndTaxUserHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<RoomDTO?>> Handle(
        GetRoomBetweenCustomerAndTaxUserQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Buscar room donde participen AMBOS: el Customer Y el TaxUser
            var query =
                from r in _context.Rooms.AsNoTracking()
                join p1 in _context.RoomParticipants.AsNoTracking() on r.Id equals p1.RoomId
                join p2 in _context.RoomParticipants.AsNoTracking() on r.Id equals p2.RoomId
                where
                    r.IsActive
                    && p1.ParticipantType == ParticipantType.Customer
                    && p1.CustomerId == request.CustomerId
                    && p1.IsActive
                    && p2.ParticipantType == ParticipantType.TaxUser
                    && p2.TaxUserId == request.TaxUserId
                    && p2.CompanyId == request.CompanyId
                    && p2.IsActive
                select r;

            // Filtrar por tipo si se especifica
            if (request.Type.HasValue)
            {
                query = query.Where(r => r.Type == request.Type.Value);
            }

            // Obtener la sala más reciente
            var room = await query
                .Include(r => r.Participants)
                .OrderByDescending(r => r.LastActivityAt ?? r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (room == null)
            {
                return new ApiResponse<RoomDTO?>(true, "No room found between users", null);
            }

            // Mapear a DTO
            var roomDto = _mapper.Map<RoomDTO>(room);

            _logger.LogInformation(
                "Found room {RoomId} between Customer {CustomerId} and TaxUser {TaxUserId}",
                room.Id,
                request.CustomerId,
                request.TaxUserId
            );

            return new ApiResponse<RoomDTO?>(true, "Room found successfully", roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error finding room between Customer {CustomerId} and TaxUser {TaxUserId}",
                request.CustomerId,
                request.TaxUserId
            );
            return new ApiResponse<RoomDTO?>(false, "Failed to find room");
        }
    }
}
