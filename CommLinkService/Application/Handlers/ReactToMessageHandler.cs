using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class ReactToMessageHandler
    : IRequestHandler<ReactToMessageCommand, ReactToMessageResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<ReactToMessageHandler> _logger;

    public ReactToMessageHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        ILogger<ReactToMessageHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ReactToMessageResult> Handle(
        ReactToMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        var message = await _context
            .Messages.Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && !m.IsDeleted, cancellationToken);

        if (message == null)
            return new ReactToMessageResult(false, "Message not found");

        // Verificar si el usuario es participante de la sala
        var isParticipant = await _context.RoomParticipants.AnyAsync(
            p => p.RoomId == message.RoomId && p.UserId == request.UserId && p.IsActive,
            cancellationToken
        );

        if (!isParticipant)
            return new ReactToMessageResult(false, "You are not a participant in this room");

        message.AddReaction(request.UserId, request.Emoji);
        await _context.SaveChangesAsync(cancellationToken);

        // Notificar a todos en la sala
        await _webSocketManager.SendToRoomAsync(
            message.RoomId,
            new
            {
                type = "message_reaction_added",
                data = new
                {
                    messageId = message.Id,
                    userId = request.UserId,
                    emoji = request.Emoji,
                },
            }
        );

        _logger.LogInformation(
            "User {UserId} reacted to message {MessageId} with {Emoji}",
            request.UserId,
            request.MessageId,
            request.Emoji
        );

        return new ReactToMessageResult(true, null);
    }
}
