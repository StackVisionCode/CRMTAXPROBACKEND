using Commands.FirmStatus;
using Common;
using Domains.Firms;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Handlers.FirmStatusHandlers
{
    public class DeleteFirmStatusHandler : IRequestHandler<DeleteFirmStatusCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DeleteFirmStatusHandler> _logger;

        public DeleteFirmStatusHandler(ApplicationDbContext dbContext, ILogger<DeleteFirmStatusHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteFirmStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _dbContext.FirmStatus.FirstOrDefaultAsync(x => x.Id == request.FirmStatusDto.Id, cancellationToken);
                if (entity == null) return new ApiResponse<bool>(false, "FirmStatus not found", false);

                _dbContext.FirmStatus.Remove(entity);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

                return new ApiResponse<bool>(result, result ? "FirmStatus deleted successfully" : "Failed to delete FirmStatus", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FirmStatus");
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
    }
}
