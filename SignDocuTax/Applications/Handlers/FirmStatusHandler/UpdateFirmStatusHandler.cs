using AutoMapper;
using Commands.FirmStatus;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.FirmStatusHandlers
{
    public class UpdateFirmStatusHandler : IRequestHandler<UpdateFirmStatusCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateFirmStatusHandler> _logger;

        public UpdateFirmStatusHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateFirmStatusHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateFirmStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _dbContext.FirmStatus.FirstOrDefaultAsync(x => x.Id == request.FirmStatus.Id, cancellationToken);
                if (entity == null) return new ApiResponse<bool>(false, "FirmStatus not found", false);

                _mapper.Map(request.FirmStatus, entity);
                _dbContext.FirmStatus.Update(entity);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

                return new ApiResponse<bool>(result, result ? "FirmStatus updated successfully" : "Failed to update FirmStatus", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FirmStatus");
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
    }
}
