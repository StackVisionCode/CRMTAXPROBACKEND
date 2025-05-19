using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record UpdateEmailConfigCommand(int Id, EmailConfigDTO Config) : IRequest<EmailConfigDTO>;