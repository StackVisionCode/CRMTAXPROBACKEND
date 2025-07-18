using Application.Common;
using Application.Dtos;
using MediatR;

namespace Infrastructure.Command.Templates;



public record class CreateTemplateCommads(CreateDto TemplateDto) : IRequest<ApiResponse<bool>>;