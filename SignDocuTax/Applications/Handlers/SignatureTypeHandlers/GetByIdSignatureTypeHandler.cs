using AutoMapper;
using Common;
using Dtos.SignatureTypeDto;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SignatureTypes;

namespace Handlers.SignatureType;

public class GetByIdSignatureTypeHandler : IRequestHandler<GetSignatureTypeByIdQuery, ApiResponse<SignatureTypeDto>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetByIdSignatureTypeHandler(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<SignatureTypeDto>> Handle(GetSignatureTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.SignatureTypes
            .FirstOrDefaultAsync(x => x.Id == request.GetById.Id, cancellationToken);

        if (entity == null)
        {
            return new ApiResponse<SignatureTypeDto>(false, "SignatureType not found", null);
        }

        var dto = _mapper.Map<SignatureTypeDto>(entity);
        return new ApiResponse<SignatureTypeDto>(true, "SignatureType retrieved successfully", dto);
    }
}
