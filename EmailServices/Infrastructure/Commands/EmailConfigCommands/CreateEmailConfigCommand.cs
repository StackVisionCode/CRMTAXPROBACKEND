using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record CreateEmailConfigCommand(EmailConfigDTO Config) : IRequest<EmailConfigDTO>;
