using Application.Common;
using Application.Dtos;
using MediatR;

namespace Infrastructure.Command.Templates;

public record class UpdateTemplateCommands(Guid IdTemplade,UpdateTempladeDto TemplateDto) : IRequest<ApiResponse<bool>>;