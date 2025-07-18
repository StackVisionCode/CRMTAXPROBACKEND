// Application/Handlers/Templates/GetAllTemplatesHandler.cs
using Application.Common;
using Application.Dtos;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Querys.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetAllTemplatesHandler : IRequestHandler<GetAllTemplatesQuery, ApiResponse<List<TemplateDto>>>
{
    private readonly TaxProStoreDbContext _db;
    private readonly IMapper _mapper;

    public GetAllTemplatesHandler(TaxProStoreDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<TemplateDto>>> Handle(GetAllTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _db.Templates
            .Where(t => t.OwnerUserId == request.OwnerUserId)
            .ToListAsync(cancellationToken);

        var result = _mapper.Map<List<TemplateDto>>(templates);
        return new ApiResponse<List<TemplateDto>>(true,"Get Template",result);
    }
}
