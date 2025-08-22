using Common;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class LeaveRoomCommand(
    Guid RoomId,
    ParticipantType ParticipantType,
    Guid? TaxUserId,
    Guid? CustomerId
) : IRequest<ApiResponse<bool>>;
