
using Application.Common;
using Application.Dtos;
using MediatR;

namespace Infrastructure.Querys.Templates;

public record GetAllTemplatesQuery(Guid OwnerUserId) : IRequest<ApiResponse<List<TemplateDto>>>;
