using Common;
using DTOs.MessageDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetRoomMessagesQuery(
    Guid RoomId,
    ParticipantType RequesterType,
    Guid? RequesterTaxUserId,
    Guid? RequesterCustomerId,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<ApiResponse<GetRoomMessagesResult>>;

public sealed record GetRoomMessagesResult(
    List<MessageDTO> Messages,
    int TotalCount,
    bool HasMore,
    int CurrentPage
);
