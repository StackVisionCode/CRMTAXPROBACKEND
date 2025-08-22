using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class JoinRoomCommand(
    Guid RoomId,
    ParticipantType ParticipantType,
    Guid? TaxUserId,
    Guid? CustomerId,
    Guid? CompanyId,
    string ConnectionId
) : IRequest<ApiResponse<RoomParticipantDTO>>;
