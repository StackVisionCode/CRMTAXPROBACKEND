using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record UpdateEmailConfigCommand(Guid Id, EmailConfigDTO Config) : IRequest<EmailConfigDTO>;
