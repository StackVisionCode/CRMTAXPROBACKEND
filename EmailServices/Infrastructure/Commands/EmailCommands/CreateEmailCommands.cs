using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record class CreateEmailCommand(EmailDTO EmailDto) : IRequest<EmailDTO>;