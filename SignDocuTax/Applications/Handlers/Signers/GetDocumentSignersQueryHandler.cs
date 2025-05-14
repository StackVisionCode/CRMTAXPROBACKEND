using AutoMapper;
using Common;
using DTOs.Signers;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Signers;

namespace Handlers.Signers;
public class GetDocumentSignersQueryHandler : IRequestHandler<GetDocumentSignersQuery, ApiResponse<List<ExternalSignerDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDocumentSignersQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ExternalSignerDto>>> Handle(GetDocumentSignersQuery request, CancellationToken cancellationToken)
    {
        var signers = await _context.ExternalSigners
            .Where(x => x.DocumentId == request.DocumentId)
            .ToListAsync(cancellationToken);

        var signerDtos = _mapper.Map<List<ExternalSignerDto>>(signers);

        return new ApiResponse<List<ExternalSignerDto>>(true, "Signers retrieved successfully", signerDtos);
    }
}
