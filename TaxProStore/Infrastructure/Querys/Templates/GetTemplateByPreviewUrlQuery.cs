// Infrastructure/Queries/Templates/GetTemplateByPreviewUrlQuery.cs
using Application.Common;
using Application.Dtos;
using MediatR;

namespace Infrastructure.Queries.Templates;

public record GetTemplateByPreviewUrlQuery(Guid PreviewUrl) : IRequest<ApiResponse<TemplateDto>>;
