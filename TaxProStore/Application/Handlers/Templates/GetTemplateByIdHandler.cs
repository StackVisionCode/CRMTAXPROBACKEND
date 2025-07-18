// Application/Handlers/Templates/GetTemplateByIdHandler.cs
using Application.Common;
using Application.Dtos;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetTemplateByIdHandler : IRequestHandler<GetTemplateByIdQuery, ApiResponse<TemplateDto>>
{
    private readonly TaxProStoreDbContext _db;
    private readonly IMapper _mapper;

    public GetTemplateByIdHandler(TaxProStoreDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TemplateDto>> Handle(GetTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _db.Templates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template == null)
            return new ApiResponse<TemplateDto>(false,"Template no encontrado");

        var dto = _mapper.Map<TemplateDto>(template);
        return new  ApiResponse<TemplateDto>(true,"Get By Id",dto);
    }
}
