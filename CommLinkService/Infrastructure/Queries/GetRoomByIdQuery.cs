using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetRoomByIdQuery(
    Guid RoomId,
    ParticipantType RequesterType,
    Guid? RequesterTaxUserId,
    Guid? RequesterCustomerId
) : IRequest<ApiResponse<RoomDTO?>>;
