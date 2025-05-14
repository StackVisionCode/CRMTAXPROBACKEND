using Application.Common.DTO;
using Common;
using MediatR;

namespace Infrastructure.Commands;

public record class CreateEmailCommands(EmailDTO Email):IRequest<ApiResponse<bool>>;
