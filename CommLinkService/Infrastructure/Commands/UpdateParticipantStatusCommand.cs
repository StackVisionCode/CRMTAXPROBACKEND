using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class UpdateParticipantStatusCommand(
    Guid RoomId,
    ParticipantType ParticipantType,
    Guid? TaxUserId,
    Guid? CustomerId,
    bool? IsMuted,
    bool? IsVideoEnabled
) : IRequest<ApiResponse<RoomParticipantDTO>>;
