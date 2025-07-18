using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class EditMessageHandler : IRequestHandler<EditMessageCommand, EditMessageResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<EditMessageHandler> _logger;

    public EditMessageHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        ILogger<EditMessageHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<EditMessageResult> Handle(
        EditMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        var message = await _context.Messages.FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.SenderId == request.UserId && !m.IsDeleted,
            cancellationToken
        );

        if (message == null)
            return new EditMessageResult(
                false,
                "Message not found or you don't have permission to edit it",
                null
            );

        message.Edit(request.NewContent);
        await _context.SaveChangesAsync(cancellationToken);

        // Notificar a todos en la sala
        await _webSocketManager.SendToRoomAsync(
            message.RoomId,
            new
            {
                type = "message_edited",
                data = new
                {
                    messageId = message.Id,
                    roomId = message.RoomId,
                    newContent = message.Content,
                    editedAt = message.EditedAt,
                },
            }
        );

        _logger.LogInformation(
            "Message {MessageId} edited by user {UserId}",
            request.MessageId,
            request.UserId
        );

        return new EditMessageResult(true, null, message.EditedAt);
    }
}
