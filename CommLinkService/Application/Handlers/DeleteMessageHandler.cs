using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class DeleteMessageHandler
    : IRequestHandler<DeleteMessageCommand, DeleteMessageResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<DeleteMessageHandler> _logger;

    public DeleteMessageHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        ILogger<DeleteMessageHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<DeleteMessageResult> Handle(
        DeleteMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        var message = await _context.Messages.FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.SenderId == request.UserId && !m.IsDeleted,
            cancellationToken
        );

        if (message == null)
            return new DeleteMessageResult(
                false,
                "Message not found or you don't have permission to delete it"
            );

        message.Delete();
        await _context.SaveChangesAsync(cancellationToken);

        // Notificar a todos en la sala
        await _webSocketManager.SendToRoomAsync(
            message.RoomId,
            new
            {
                type = "message_deleted",
                data = new { messageId = message.Id, roomId = message.RoomId },
            }
        );

        _logger.LogInformation(
            "Message {MessageId} deleted by user {UserId}",
            request.MessageId,
            request.UserId
        );

        return new DeleteMessageResult(true, null);
    }
}
