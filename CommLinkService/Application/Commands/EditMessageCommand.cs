using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record EditMessageCommand(Guid MessageId, Guid UserId, string NewContent)
    : IRequest<EditMessageResult>;

public sealed record EditMessageResult(bool Success, string? ErrorMessage, DateTime? EditedAt);
