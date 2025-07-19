using CommLinkService.Application.DTOs;
using CommLinkService.Domain.Entities;
using MediatR;

namespace CommLinkService.Application.Queries;

public sealed record GetRoomBetweenUsersQuery(
    Guid User1Id,
    Guid User2Id,
    RoomType? Type /* null => cualquiera */
) : IRequest<RoomInfoDto?>;
