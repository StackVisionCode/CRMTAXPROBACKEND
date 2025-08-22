using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public record class GetRoomBetweenCustomerAndTaxUserQuery(
    Guid CustomerId,
    Guid TaxUserId,
    Guid CompanyId,
    RoomType? Type = null
) : IRequest<ApiResponse<RoomDTO?>>;
