// Infrastructure/Handlers/Templates/GetTemplateByPreviewUrlQueryHandler.cs
using Application.Common;
using Application.Dtos;
using Infrastructure.Context;
using Infrastructure.Queries.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Handlers.Templates;

public class GetTemplateByPreviewUrlQueryHandler : IRequestHandler<GetTemplateByPreviewUrlQuery, ApiResponse<TemplateDto>>
{
    private readonly TaxProStoreDbContext _context;

    public GetTemplateByPreviewUrlQueryHandler(TaxProStoreDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TemplateDto>> Handle(GetTemplateByPreviewUrlQuery request, CancellationToken cancellationToken)
    {
        var template = await _context.Templates
            .AsNoTracking()
            .Where(t => t.Id == request.PreviewUrl)
            .Select(t => new TemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                HtmlContent = t.HtmlContent,
                OwnerUserId = t.OwnerUserId,
                CreatedAt = t.CreatedAt,
                PreviewUrl = t.PreviewUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
            return new  ApiResponse<TemplateDto>(false,"Template not found");

        return new ApiResponse<TemplateDto>(true,"Template completo",template);
    }
}
