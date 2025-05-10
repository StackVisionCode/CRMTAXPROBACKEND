using AutoMapper;
using Common;
using DTOs.FirmsDto;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Firms;

namespace Handlers.Firms
{
    public class GetAllFirmsHandler : IRequestHandler<GetAllFirmsQuery, ApiResponse<List<ReadFirmDto>>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllFirmsHandler> _logger;

        public GetAllFirmsHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllFirmsHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<ReadFirmDto>>> Handle(GetAllFirmsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var firms = await _dbContext.Firms
                    .Include(f => f.SignatureType)
                    .Include(f => f.FirmStatus)
                    .ToListAsync(cancellationToken: cancellationToken);
                if (firms == null || firms.Count == 0)
                {
                    return new ApiResponse<List<ReadFirmDto>>(false, "No firms found", null!);
                }
                var firmDtos = _mapper.Map<List<ReadFirmDto>>(firms);
                return new ApiResponse<List<ReadFirmDto>>(true, "Firms retrieved successfully", firmDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving firms: {Message}", ex.Message);
                return new ApiResponse<List<ReadFirmDto>>(false, ex.Message, null!);
            }
        }
    }
}