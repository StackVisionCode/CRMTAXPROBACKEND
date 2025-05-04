using AuthService.DTOs.PermissionDTOs;
using Common;
using MediatR;

namespace Commands.PermissionCommands;

public record class CreatePermissionCommands(PermissionDTO Permission) : IRequest<ApiResponse<bool>>;