using Common;
using DTOs.VideoCallDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetActiveVideoCallsQuery(
    ParticipantType UserType,
    Guid? TaxUserId,
    Guid? CustomerId,
    Guid? CompanyId
) : IRequest<ApiResponse<List<ActiveVideoCallDTO>>>;
