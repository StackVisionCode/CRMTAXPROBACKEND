using AutoMapper;
using Common;
using Dtos.SignatureTypeDto;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SignatureTypes;

namespace Handlers.SignatureType;

public class GetAllSignatureTypeHandler : IRequestHandler<GetAllSignatureTypeQuery, ApiResponse<IEnumerable<SignatureTypeDto>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetAllSignatureTypeHandler(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<SignatureTypeDto>>> Handle(GetAllSignatureTypeQuery request, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.SignatureTypes.ToListAsync(cancellationToken);
        var dto = _mapper.Map<IEnumerable<SignatureTypeDto>>(entities);

        return new ApiResponse<IEnumerable<SignatureTypeDto>>(true, "SignatureTypes retrieved successfully", dto);
    }
}
