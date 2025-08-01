using CommLinkService.Application.Events;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace CommLinkService.Application.Handlers;

public sealed class SendMessageHandler : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        IEventBus eventBus,
        ILogger<SendMessageHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<SendMessageResult> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        var room = await _context
            .Rooms.Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

        if (room == null)
            throw new InvalidOperationException("Room not found");

        if (!room.Participants.Any(p => p.UserId == request.SenderId && p.IsActive))
            throw new UnauthorizedAccessException("User is not a participant in this room");

        var message = new Message(
            request.RoomId,
            request.SenderId,
            request.Content,
            request.Type,
            request.Metadata
        );

        room.AddMessage(message);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        // Send to all participants via WebSocket
        var messageData = new
        {
            type = "message",
            data = new
            {
                messageId = message.Id,
                roomId = message.RoomId,
                senderId = message.SenderId,
                content = message.Content,
                messageType = message.Type.ToString(),
                sentAt = message.SentAt,
                metadata = message.Metadata,
            },
        };

        foreach (var participant in room.Participants.Where(p => p.IsActive))
        {
            await _webSocketManager.SendToUserAsync(participant.UserId, messageData);
        }

        // Publish event
        _eventBus.Publish(
            new MessageSentEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                message.Id,
                message.RoomId,
                message.SenderId,
                message.Content,
                message.Type
            )
        );

        _logger.LogInformation(
            "Message {MessageId} sent in room {RoomId}",
            message.Id,
            request.RoomId
        );

        return new SendMessageResult(
            message.Id,
            message.RoomId,
            message.SenderId,
            message.Content,
            message.Type,
            message.SentAt
        );
    }
}
