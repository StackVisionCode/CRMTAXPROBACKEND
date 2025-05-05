using Commands.Firms;
using Common;
using DTOs.FirmsDto;
using MediatR;
using AutoMapper;
using Domains.Firms;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Infraestructure.Context;
using Queries.Firms;
namespace Handlers.Firms
{
    public class GetFirmByIdHandler : IRequestHandler<GetFirmByIdQuery, ApiResponse<ReadFirmDto>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<GetFirmByIdHandler> _logger;

        public GetFirmByIdHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetFirmByIdHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<ReadFirmDto>> Handle(GetFirmByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var firm = await _dbContext.Firms.Include(d => d.FirmStatus).Include(d => d.SignatureType).FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
                if (firm == null)
                {
                    return new ApiResponse<ReadFirmDto>(false, "Firm not found", null!);
                }

                var firmDto = _mapper.Map<ReadFirmDto>(firm);
                return new ApiResponse<ReadFirmDto>(true, "Firm retrieved successfully", firmDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving firm: {Message}", ex.Message);
                return new ApiResponse<ReadFirmDto>(false, ex.Message, null!);
            }
        }
    }
}