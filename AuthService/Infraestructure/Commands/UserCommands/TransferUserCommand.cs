using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

// TRANSFERIR USUARIO (solo Developers)
public record TransferUserCommand(Guid UserId, Guid TargetCompanyId)
    : IRequest<ApiResponse<UserGetDTO>>;
