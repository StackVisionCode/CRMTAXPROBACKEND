using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record class SendEmailCommand(Guid EmailId, Guid? UserId) : IRequest<EmailDTO>;
