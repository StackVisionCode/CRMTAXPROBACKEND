using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record EndVideoCallCommand(Guid RoomId, Guid UserId, Guid CallId)
    : IRequest<EndVideoCallResult>;

public sealed record EndVideoCallResult(bool Success, DateTime EndedAt);
