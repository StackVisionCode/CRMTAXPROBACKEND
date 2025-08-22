using Common;
using DTOs.ConnectionDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetActiveConnectionsQuery(
    ParticipantType UserType,
    Guid? TaxUserId,
    Guid? CustomerId,
    Guid? CompanyId
) : IRequest<ApiResponse<List<ConnectionDTO>>>;
