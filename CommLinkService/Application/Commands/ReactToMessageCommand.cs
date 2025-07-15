using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record ReactToMessageCommand(Guid MessageId, Guid UserId, string Emoji)
    : IRequest<ReactToMessageResult>;

public sealed record ReactToMessageResult(bool Success, string? ErrorMessage);
