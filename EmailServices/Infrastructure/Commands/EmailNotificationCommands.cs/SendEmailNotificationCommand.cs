using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record SendEmailNotificationCommand(EmailNotificationDto Payload) : IRequest<Unit>;
