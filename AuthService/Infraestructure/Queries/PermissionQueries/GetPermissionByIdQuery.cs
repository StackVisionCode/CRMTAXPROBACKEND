using AuthService.DTOs.PermissionDTOs;
using Common;
using MediatR;

namespace Commands.PermissionCommands;

public record class GetPermissionByIdQuery(int PermissionId) : IRequest<ApiResponse<PermissionDTO>>;