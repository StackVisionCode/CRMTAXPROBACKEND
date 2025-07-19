using Application.Common;
using Application.Dtos.Form;
using AutoMapper;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Querys.Form;

public class GetAllFormInstancesHandler : IRequestHandler<GetAllFormInstancesQuery, ApiResponse<List<FormInstanceDto>>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;

    public GetAllFormInstancesHandler(TaxProStoreDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<FormInstanceDto>>> Handle(GetAllFormInstancesQuery request, CancellationToken cancellationToken)
    {
        var instances = await _context.FormInstances
            .Include(x => x.Responses)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<FormInstanceDto>>(instances);
        return new ApiResponse<List<FormInstanceDto>>(true,"forminstance",dtos);
    }
}
