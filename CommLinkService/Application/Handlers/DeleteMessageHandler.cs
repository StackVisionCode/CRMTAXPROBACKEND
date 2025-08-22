using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class DeleteMessageHandler : IRequestHandler<DeleteMessageCommand, ApiResponse<bool>>
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

    public async Task<ApiResponse<bool>> Handle(
        DeleteMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Buscar mensaje con verificación de ownership
            var message = await _context.Messages.FirstOrDefaultAsync(
                m =>
                    m.Id == request.MessageId
                    && !m.IsDeleted
                    && (
                        (
                            request.DeleterType == ParticipantType.TaxUser
                            && m.SenderTaxUserId == request.DeleterTaxUserId
                        )
                        || (
                            request.DeleterType == ParticipantType.Customer
                            && m.SenderCustomerId == request.DeleterCustomerId
                        )
                    ),
                cancellationToken
            );

            if (message == null)
                return new ApiResponse<bool>(
                    false,
                    "Message not found or you don't have permission to delete it"
                );

            // Marcar como eliminado
            message.IsDeleted = true;
            message.Content = "[Message deleted]";
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Notificar por WebSocket
            await _webSocketManager.SendToRoomAsync(
                message.RoomId,
                new
                {
                    type = "message_deleted",
                    data = new { messageId = message.Id, roomId = message.RoomId },
                }
            );

            _logger.LogInformation(
                "Message {MessageId} deleted by {DeleterType} {DeleterId}",
                request.MessageId,
                request.DeleterType,
                request.DeleterType == ParticipantType.TaxUser
                    ? request.DeleterTaxUserId
                    : request.DeleterCustomerId
            );

            return new ApiResponse<bool>(true, "Message deleted successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message");
            return new ApiResponse<bool>(false, "Failed to delete message");
        }
    }
}
