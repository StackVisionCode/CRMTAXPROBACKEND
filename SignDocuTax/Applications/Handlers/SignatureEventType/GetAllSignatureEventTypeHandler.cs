using AutoMapper;
using Common;
using DTOs.SignatureEventTypeDto;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SignatureEventType;

namespace Handlers.SignatureEventType
{
    public class GetAllSignatureEventTypeHandler : IRequestHandler<GetAllSignatureEventTypeQuery, ApiResponse<List<ReadSignatureEventTypeDto>>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetAllSignatureEventTypeHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ReadSignatureEventTypeDto>>> Handle(GetAllSignatureEventTypeQuery request, CancellationToken cancellationToken)
        {
            var list = await _context.SignatureEventTypes.ToListAsync(cancellationToken);
            var dtoList = _mapper.Map<List<ReadSignatureEventTypeDto>>(list);
               return new ApiResponse<List<ReadSignatureEventTypeDto>>(true, "ReadSignatureEventType retrieved successfully", dtoList); 
        }
    }
}
