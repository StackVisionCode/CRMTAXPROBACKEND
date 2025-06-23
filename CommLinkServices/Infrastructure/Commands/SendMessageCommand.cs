using CommLinkServices.Application.DTOs;
using Common;
using MediatR;

namespace CommLinkServices.Infrastructure.Commands;

public record class SendMessageCommand(
    Guid ConversationId,
    Guid SenderId,
    SendMessageRequestDto Payload
) : IRequest<ApiResponse<MessageDto>>;
