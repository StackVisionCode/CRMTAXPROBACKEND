using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public record class GetTaxUserRoomsQuery(Guid TaxUserId, Guid CompanyId)
    : IRequest<ApiResponse<List<RoomDTO>>>;
