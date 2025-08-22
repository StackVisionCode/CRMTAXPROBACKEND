using Common;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetUnreadMessageCountQuery(
    Guid RoomId,
    ParticipantType UserType,
    Guid? TaxUserId,
    Guid? CustomerId
) : IRequest<ApiResponse<int>>;
