using Commands.Firms;
using Common;
using DTOs.FirmsDto;
using MediatR;
using AutoMapper;
using Domains.Firms;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Infraestructure.Context;
namespace Handlers.Firms
{
    public class DeleteFirmHandler : IRequestHandler<DeleteFirmCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DeleteFirmHandler> _logger;

        public DeleteFirmHandler(ApplicationDbContext dbContext, ILogger<DeleteFirmHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteFirmCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var firm = await _dbContext.Firms.FirstOrDefaultAsync(x => x.Id == request.Firm.Id, cancellationToken);
                if (firm == null)
                {
                    return new ApiResponse<bool>(false, "Firm not found", false);
                }

                _dbContext.Firms.Remove(firm);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
                return new ApiResponse<bool>(result, result ? "Firm deleted successfully" : "Failed to delete firm", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting firm: {Message}", ex.Message);
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
    }
}