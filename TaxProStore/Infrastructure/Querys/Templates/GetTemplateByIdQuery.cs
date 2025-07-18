// Infrastructure/Queries/Templates/GetTemplateByIdQuery.cs
using Application.Common;
using Application.Dtos;
using MediatR;

namespace Infrastructure.Queries.Templates;

public record GetTemplateByIdQuery(Guid TemplateId) : IRequest<ApiResponse<TemplateDto>>;
