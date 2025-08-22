using AutoMapper;
using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.MessageDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetRoomMessagesHandler
    : IRequestHandler<GetRoomMessagesQuery, ApiResponse<GetRoomMessagesResult>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetRoomMessagesHandler> _logger;

    public GetRoomMessagesHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IHttpClientFactory httpClientFactory,
        ILogger<GetRoomMessagesHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ApiResponse<GetRoomMessagesResult>> Handle(
        GetRoomMessagesQuery request,
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
                return new ApiResponse<GetRoomMessagesResult>(
                    false,
                    "You are not a participant in this room"
                );

            // Contar total de mensajes
            var totalCount = await _context.Messages.CountAsync(
                m => m.RoomId == request.RoomId && !m.IsDeleted,
                cancellationToken
            );

            // Obtener mensajes paginados
            var messages = await _context
                .Messages.Include(m => m.Reactions)
                .Where(m => m.RoomId == request.RoomId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Mapear a DTOs
            var messageDtos = _mapper.Map<List<MessageDTO>>(messages);

            // Calcular si hay más páginas
            var hasMore = totalCount > request.PageNumber * request.PageSize;

            var result = new GetRoomMessagesResult(
                messageDtos,
                totalCount,
                hasMore,
                request.PageNumber
            );

            return new ApiResponse<GetRoomMessagesResult>(
                true,
                "Messages retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room messages for room {RoomId}", request.RoomId);
            return new ApiResponse<GetRoomMessagesResult>(false, "Failed to get room messages");
        }
    }
}
