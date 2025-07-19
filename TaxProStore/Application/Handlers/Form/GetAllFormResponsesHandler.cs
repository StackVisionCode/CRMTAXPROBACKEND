using Application.Common;
using Application.Dtos.Form;
using AutoMapper;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetAllFormResponsesHandler : IRequestHandler<GetAllFormResponsesQuery, ApiResponse<List<FormResponseDto>>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;

    public GetAllFormResponsesHandler(TaxProStoreDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<FormResponseDto>>> Handle(GetAllFormResponsesQuery request, CancellationToken cancellationToken)
    {
        var responses = await _context.FormResponses.ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<FormResponseDto>>(responses);
        return new ApiResponse<List<FormResponseDto>>(true,"Form response Dto",dtos);
    }
}
