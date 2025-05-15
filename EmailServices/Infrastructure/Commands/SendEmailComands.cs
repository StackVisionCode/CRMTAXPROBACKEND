using Application.Common.DTO;
using Common;
using MediatR;

namespace Infrastructure.Commands;

public record class SendEmailCommands(EmailDTO email):IRequest<ApiResponse<bool>>;