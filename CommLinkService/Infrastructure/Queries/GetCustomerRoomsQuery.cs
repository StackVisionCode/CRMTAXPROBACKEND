using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Queries;

public record class GetCustomerRoomsQuery(Guid CustomerId) : IRequest<ApiResponse<List<RoomDTO>>>;
