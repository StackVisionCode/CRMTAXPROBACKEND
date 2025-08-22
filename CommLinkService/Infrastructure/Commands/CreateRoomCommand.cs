using Common;
using DTOs.RoomDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class CreateRoomCommand(CreateRoomDTO RoomData) : IRequest<ApiResponse<RoomDTO>>;
