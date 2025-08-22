using Common;
using MediatR;

namespace CommLinkService.Infrastructure.Queries;

public record class GetActiveCallsQuery(ParticipantType UserType, Guid? TaxUserId, Guid? CustomerId)
    : IRequest<ApiResponse<List<ActiveCallDto>>>;

public record class ActiveCallDto(
    Guid CallId,
    Guid RoomId,
    string RoomName,
    DateTime StartedAt,
    List<Guid> ParticipantIds
);
