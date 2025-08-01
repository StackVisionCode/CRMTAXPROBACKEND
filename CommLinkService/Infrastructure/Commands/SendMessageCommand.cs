using CommLinkService.Domain.Entities;
using MediatR;

namespace CommLinkService.Infrastructure.Commands;

public sealed record SendMessageCommand(
    Guid RoomId,
    Guid SenderId,
    string Content,
    MessageType Type,
    string? Metadata = null
) : IRequest<SendMessageResult>;

public sealed record SendMessageResult(
    Guid MessageId,
    Guid RoomId,
    Guid SenderId,
    string Content,
    MessageType Type,
    DateTime SentAt
);
