using CommLinkService.Domain.Entities;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetRoomMessagesQuery(Guid RoomId, int PageNumber = 1, int PageSize = 50)
    : IRequest<GetRoomMessagesResult>;

public sealed record GetRoomMessagesResult(List<MessageDto> Messages, int TotalCount, bool HasMore);

public sealed record MessageDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string Content,
    MessageType Type,
    DateTime SentAt,
    bool IsEdited,
    List<ReactionDto> Reactions
);

public sealed record ReactionDto(string Emoji, List<Guid> UserIds);
