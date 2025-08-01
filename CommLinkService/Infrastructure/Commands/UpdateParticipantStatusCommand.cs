using MediatR;

namespace CommLinkService.Infrastructure.Commands;

public sealed record UpdateParticipantStatusCommand(
    Guid RoomId,
    Guid UserId,
    bool? IsMuted,
    bool? IsVideoEnabled
) : IRequest<UpdateParticipantStatusResult>;

public sealed record UpdateParticipantStatusResult(bool Success, string? ErrorMessage);
