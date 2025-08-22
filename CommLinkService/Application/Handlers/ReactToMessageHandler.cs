using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.MessageDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class ReactToMessageHandler
    : IRequestHandler<ReactToMessageCommand, ApiResponse<MessageReactionDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<ReactToMessageHandler> _logger;

    public ReactToMessageHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        ILogger<ReactToMessageHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ApiResponse<MessageReactionDTO>> Handle(
        ReactToMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el mensaje existe
            var message = await _context
                .Messages.Include(m => m.Reactions)
                .FirstOrDefaultAsync(
                    m => m.Id == request.MessageId && !m.IsDeleted,
                    cancellationToken
                );

            if (message == null)
                return new ApiResponse<MessageReactionDTO>(false, "Message not found");

            // Verificar que el usuario es participante de la sala
            bool isParticipant = false;
            if (request.ReactorType == ParticipantType.TaxUser)
            {
                isParticipant = await _context.RoomParticipants.AnyAsync(
                    p =>
                        p.RoomId == message.RoomId
                        && p.ParticipantType == ParticipantType.TaxUser
                        && p.TaxUserId == request.ReactorTaxUserId
                        && p.IsActive,
                    cancellationToken
                );
            }
            else if (request.ReactorType == ParticipantType.Customer)
            {
                isParticipant = await _context.RoomParticipants.AnyAsync(
                    p =>
                        p.RoomId == message.RoomId
                        && p.ParticipantType == ParticipantType.Customer
                        && p.CustomerId == request.ReactorCustomerId
                        && p.IsActive,
                    cancellationToken
                );
            }

            if (!isParticipant)
                return new ApiResponse<MessageReactionDTO>(
                    false,
                    "You are not a participant in this room"
                );

            // Verificar si ya existe la reacción
            var existingReaction = message.Reactions.FirstOrDefault(r =>
                r.Emoji == request.Emoji
                && (
                    (
                        request.ReactorType == ParticipantType.TaxUser
                        && r.ReactorTaxUserId == request.ReactorTaxUserId
                    )
                    || (
                        request.ReactorType == ParticipantType.Customer
                        && r.ReactorCustomerId == request.ReactorCustomerId
                    )
                )
            );

            if (existingReaction != null)
                return new ApiResponse<MessageReactionDTO>(
                    false,
                    "You have already reacted with this emoji"
                );

            // Crear nueva reacción
            var reaction = new MessageReaction
            {
                Id = Guid.NewGuid(),
                MessageId = request.MessageId,
                ReactorType = request.ReactorType,
                ReactorTaxUserId = request.ReactorTaxUserId,
                ReactorCustomerId = request.ReactorCustomerId,
                ReactorCompanyId = request.ReactorCompanyId,
                Emoji = request.Emoji,
                ReactedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync(cancellationToken);

            // Mapear a DTO
            var reactionDto = _mapper.Map<MessageReactionDTO>(reaction);

            // Notificar por WebSocket
            await _webSocketManager.SendToRoomAsync(
                message.RoomId,
                new
                {
                    type = "message_reaction_added",
                    data = new
                    {
                        messageId = message.Id,
                        reactorType = request.ReactorType,
                        reactorTaxUserId = request.ReactorTaxUserId,
                        reactorCustomerId = request.ReactorCustomerId,
                        emoji = request.Emoji,
                    },
                }
            );

            _logger.LogInformation(
                "{ReactorType} {ReactorId} reacted to message {MessageId} with {Emoji}",
                request.ReactorType,
                request.ReactorType == ParticipantType.TaxUser
                    ? request.ReactorTaxUserId
                    : request.ReactorCustomerId,
                request.MessageId,
                request.Emoji
            );

            return new ApiResponse<MessageReactionDTO>(
                true,
                "Reaction added successfully",
                reactionDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction");
            return new ApiResponse<MessageReactionDTO>(false, "Failed to add reaction");
        }
    }
}
