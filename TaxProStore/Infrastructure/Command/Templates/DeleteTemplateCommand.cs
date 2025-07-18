using Application.Common;
using MediatR;

namespace Infrastructure.Command.Templates;

public record DeleteTemplateCommand(Guid IdTemplate) : IRequest<ApiResponse<bool>>;
