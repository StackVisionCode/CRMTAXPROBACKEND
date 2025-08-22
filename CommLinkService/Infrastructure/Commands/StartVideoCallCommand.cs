using Common;
using DTOs.VideoCallDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class StartVideoCallCommand(
    Guid RoomId,
    ParticipantType InitiatorType,
    Guid? InitiatorTaxUserId,
    Guid? InitiatorCustomerId,
    Guid? InitiatorCompanyId
) : IRequest<ApiResponse<VideoCallDTO>>;
