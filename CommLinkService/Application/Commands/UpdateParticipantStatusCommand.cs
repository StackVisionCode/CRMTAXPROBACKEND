using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record UpdateParticipantStatusCommand(
    Guid RoomId,
    Guid UserId,
    bool? IsMuted,
    bool? IsVideoEnabled
) : IRequest<UpdateParticipantStatusResult>;

public sealed record UpdateParticipantStatusResult(bool Success, string? ErrorMessage);
