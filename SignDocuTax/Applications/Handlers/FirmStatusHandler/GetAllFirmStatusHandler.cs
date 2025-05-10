using AutoMapper;
using Common;
using Dtos.FirmsStatusDto;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.FirmStatus;


namespace Handlers.FirmStatusHandlers
{
    public class GetAllFirmStatusHandler : IRequestHandler<GetAllFirmStatusQuery, ApiResponse<IEnumerable<FirmStatusDto>>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetAllFirmStatusHandler(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<FirmStatusDto>>> Handle(GetAllFirmStatusQuery request, CancellationToken cancellationToken)
        {
            var entities = await _dbContext.FirmStatus.ToListAsync(cancellationToken);
            var dtos = _mapper.Map<IEnumerable<FirmStatusDto>>(entities);
            return new ApiResponse<IEnumerable<FirmStatusDto>>(true, "FirmStatus list", dtos);
        }
    }
}
