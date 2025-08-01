using MediatR;

namespace CommLinkService.Infrastructure.Commands;

public sealed record ReactToMessageCommand(Guid MessageId, Guid UserId, string Emoji)
    : IRequest<ReactToMessageResult>;

public sealed record ReactToMessageResult(bool Success, string? ErrorMessage);
