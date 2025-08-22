using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetCompanyCustomerRoomsQuery(
    Guid CompanyId,
    Guid? CustomerId = null // Si es null, trae todos los rooms con customers
) : IRequest<ApiResponse<List<RoomDTO>>>;
