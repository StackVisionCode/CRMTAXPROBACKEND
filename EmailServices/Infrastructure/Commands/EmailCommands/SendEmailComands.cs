using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record class SendEmailCommand(int EmailId, int? UserId) : IRequest<EmailDTO>;
