using AutoMapper;
using Common;
using Dtos.FirmsStatusDto;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.FirmStatus;

namespace Handlers.FirmStatusHandlers
{
    public class GetFirmStatusByIdHandler : IRequestHandler<GetFirmStatusByIdQuery, ApiResponse<FirmStatusDto>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetFirmStatusByIdHandler(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<ApiResponse<FirmStatusDto>> Handle(GetFirmStatusByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.FirmStatus.FirstOrDefaultAsync(x => x.Id == request.FirmStatus.Id, cancellationToken);
            if (entity == null) return new ApiResponse<FirmStatusDto>(false, "FirmStatus not found", null);

            var dto = _mapper.Map<FirmStatusDto>(entity);
            return new ApiResponse<FirmStatusDto>(true, "FirmStatus found", dto);
        }
    }
}
