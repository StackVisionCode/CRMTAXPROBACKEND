using MediatR;

namespace CommLinkService.Infrastructure.Commands;

public sealed record EditMessageCommand(Guid MessageId, Guid UserId, string NewContent)
    : IRequest<EditMessageResult>;

public sealed record EditMessageResult(bool Success, string? ErrorMessage, DateTime? EditedAt);
