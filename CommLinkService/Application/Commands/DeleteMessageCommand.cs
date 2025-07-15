using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record DeleteMessageCommand(Guid MessageId, Guid UserId)
    : IRequest<DeleteMessageResult>;

public sealed record DeleteMessageResult(bool Success, string? ErrorMessage);
