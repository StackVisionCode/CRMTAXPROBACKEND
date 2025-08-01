using MediatR;

namespace CommLinkService.Infrastructure.Commands;

public sealed record DeleteMessageCommand(Guid MessageId, Guid UserId)
    : IRequest<DeleteMessageResult>;

public sealed record DeleteMessageResult(bool Success, string? ErrorMessage);
