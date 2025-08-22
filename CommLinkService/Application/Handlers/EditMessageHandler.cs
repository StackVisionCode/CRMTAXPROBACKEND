using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Common;
using DTOs.MessageDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class EditMessageHandler
    : IRequestHandler<EditMessageCommand, ApiResponse<MessageDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<EditMessageHandler> _logger;

    public EditMessageHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IWebSocketManager webSocketManager,
        ILogger<EditMessageHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<ApiResponse<MessageDTO>> Handle(
        EditMessageCommand request,
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
                            request.EditorType == ParticipantType.TaxUser
                            && m.SenderTaxUserId == request.EditorTaxUserId
                        )
                        || (
                            request.EditorType == ParticipantType.Customer
                            && m.SenderCustomerId == request.EditorCustomerId
                        )
                    ),
                cancellationToken
            );

            if (message == null)
                return new ApiResponse<MessageDTO>(
                    false,
                    "Message not found or you don't have permission to edit it"
                );

            // Actualizar mensaje
            message.Content = request.NewContent;
            message.EditedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Mapear a DTO
            var messageDto = _mapper.Map<MessageDTO>(message);

            // Notificar por WebSocket
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
                "Message {MessageId} edited by {EditorType} {EditorId}",
                request.MessageId,
                request.EditorType,
                request.EditorType == ParticipantType.TaxUser
                    ? request.EditorTaxUserId
                    : request.EditorCustomerId
            );

            return new ApiResponse<MessageDTO>(true, "Message edited successfully", messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message");
            return new ApiResponse<MessageDTO>(false, "Failed to edit message");
        }
    }
}
