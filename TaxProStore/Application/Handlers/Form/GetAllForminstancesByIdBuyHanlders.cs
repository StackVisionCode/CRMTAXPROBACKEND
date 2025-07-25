

using Application.Common;
using Application.Dtos.Form;
using AutoMapper;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Querys.Form;

public class GetAllForminstancesByIdBuyHanlders : IRequestHandler<GetAllForminstacesQueryByIdBuild, ApiResponse<List<FormInstanceDto>>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;

    public GetAllForminstancesByIdBuyHanlders(TaxProStoreDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<FormInstanceDto>>> Handle(GetAllForminstacesQueryByIdBuild request, CancellationToken cancellationToken)
    {
        var instances = await _context.FormInstances
            .Where(x => x.OwnerUserId == request.Id)
            .Include(x => x.Responses)
            .Include(x => x.Template)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<FormInstanceDto>>(instances);
        return new ApiResponse<List<FormInstanceDto>>(true, "forminstance", dtos);
    }
}