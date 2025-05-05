using AutoMapper;
using Common;
using DTOs.StatusRequiremtDto;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.StatusRequirement;

namespace Handlers.StatusRequirementHandlers;

    public class GetAllStatusRequirementHandler : IRequestHandler<GetAlllStatusRequirementQuery, ApiResponse<List<ReadRequiremenStatustDtos>>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetAllStatusRequirementHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ReadRequiremenStatustDtos>>> Handle(GetAlllStatusRequirementQuery request, CancellationToken cancellationToken)
        {
            var list = await _context.StatusRequirements.ToListAsync(cancellationToken);
            var dtoList = _mapper.Map<List<ReadRequiremenStatustDtos>>(list);
              return new ApiResponse<List<ReadRequiremenStatustDtos>>(true, "Documents retrieved successfully", dtoList); 
        }

}

