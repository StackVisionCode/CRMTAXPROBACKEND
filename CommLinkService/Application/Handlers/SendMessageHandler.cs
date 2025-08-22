using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Application.Events;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.MessageDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace CommLinkService.Application.Handlers;

public sealed class SendMessageHandler
    : IRequestHandler<SendMessageCommand, ApiResponse<MessageDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        IEventBus eventBus,
        ILogger<SendMessageHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ApiResponse<MessageDTO>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el room existe
            var room = await _context
                .Rooms.Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == request.MessageData.RoomId, cancellationToken);

            if (room == null)
                return new ApiResponse<MessageDTO>(false, "Room not found");

            // Verificar que el sender es participante activo
            bool isParticipant = false;
            if (request.MessageData.SenderType == ParticipantType.TaxUser)
            {
                isParticipant = room.Participants.Any(p =>
                    p.ParticipantType == ParticipantType.TaxUser
                    && p.TaxUserId == request.MessageData.SenderTaxUserId
                    && p.IsActive
                );
            }
            else if (request.MessageData.SenderType == ParticipantType.Customer)
            {
                isParticipant = room.Participants.Any(p =>
                    p.ParticipantType == ParticipantType.Customer
                    && p.CustomerId == request.MessageData.SenderCustomerId
                    && p.IsActive
                );
            }

            if (!isParticipant)
                return new ApiResponse<MessageDTO>(false, "User is not a participant in this room");

            // Crear mensaje con nueva estructura
            var message = new Message
            {
                Id = Guid.NewGuid(),
                RoomId = request.MessageData.RoomId,
                SenderType = request.MessageData.SenderType,
                SenderTaxUserId = request.MessageData.SenderTaxUserId,
                SenderCustomerId = request.MessageData.SenderCustomerId,
                SenderCompanyId = request.MessageData.SenderCompanyId,
                Content = request.MessageData.Content,
                Type = request.MessageData.Type,
                Metadata = request.MessageData.Metadata,
                SentAt = DateTime.UtcNow,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Messages.Add(message);

            // Actualizar LastActivity del room
            room.LastActivityAt = DateTime.UtcNow;
            room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Mapear a DTO
            var messageDto = _mapper.Map<MessageDTO>(message);

            // Enviar por WebSocket usando nuevo método
            await _webSocketManager.SendToRoomAsync(
                request.MessageData.RoomId,
                new { type = "new_message", data = messageDto }
            );

            // Publicar evento
            var senderId =
                request.MessageData.SenderType == ParticipantType.TaxUser
                    ? request.MessageData.SenderTaxUserId
                    : request.MessageData.SenderCustomerId;

            _eventBus.Publish(
                new MessageSentEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    message.Id,
                    message.RoomId,
                    senderId!.Value,
                    message.Content,
                    message.Type
                )
            );

            _logger.LogInformation(
                "Message {MessageId} sent in room {RoomId} by {SenderType} {SenderId}",
                message.Id,
                request.MessageData.RoomId,
                request.MessageData.SenderType,
                senderId
            );

            return new ApiResponse<MessageDTO>(true, "Message sent successfully", messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return new ApiResponse<MessageDTO>(false, "Failed to send message");
        }
    }
}
