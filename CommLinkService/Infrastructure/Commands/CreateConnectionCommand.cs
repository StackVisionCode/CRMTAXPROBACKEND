using Common;
using DTOs.ConnectionDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class CreateConnectionCommand(
    ParticipantType UserType,
    Guid? TaxUserId,
    Guid? CustomerId,
    Guid? CompanyId,
    string ConnectionId,
    string? UserAgent,
    string? IpAddress
) : IRequest<ApiResponse<ConnectionDTO>>;
