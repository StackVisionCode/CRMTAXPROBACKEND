using AuthService.DTOs.PermissionDTOs;
using Common;
using MediatR;

namespace Commands.PermissionCommands;

public record class GetAllPermissionQuery() : IRequest<ApiResponse<List<PermissionDTO>>>;
