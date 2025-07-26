using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record UpdateEmailCommand(Guid Id, EmailDTO EmailDto) : IRequest<EmailDTO>;
