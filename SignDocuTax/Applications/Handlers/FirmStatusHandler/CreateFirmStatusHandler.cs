using AutoMapper;
using Commands.FirmStatus;
using Common;
using Domains.Firms;
using Infraestructure.Context;
using MediatR;

namespace Handlers.FirmStatusHandlers
{
    public class CreateFirmStatusHandler : IRequestHandler<CreateFirmStatusCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateFirmStatusHandler> _logger;

        public CreateFirmStatusHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateFirmStatusHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(CreateFirmStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = _mapper.Map<FirmStatus>(request.FirmStatus);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.DeleteAt = null;
                await _dbContext.FirmStatus.AddAsync(entity, cancellationToken);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

                return new ApiResponse<bool>(result, result ? "FirmStatus created successfully" : "Failed to create FirmStatus", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FirmStatus");
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
    }
}
