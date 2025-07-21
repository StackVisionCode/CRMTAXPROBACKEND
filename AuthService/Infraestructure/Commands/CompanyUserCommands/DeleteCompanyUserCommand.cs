using Common;
using MediatR;

namespace Commands.CompanyUserCommands;

public record class DeleteCompanyUserCommand(Guid CompanyUserId) : IRequest<ApiResponse<bool>>;
