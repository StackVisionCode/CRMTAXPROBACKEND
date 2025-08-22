using Common;
using DTOs.VideoCallDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class EndVideoCallCommand(
    Guid RoomId,
    ParticipantType EndedByType,
    Guid? EndedByTaxUserId,
    Guid? EndedByCustomerId,
    Guid CallId
) : IRequest<ApiResponse<VideoCallEndDTO>>;
