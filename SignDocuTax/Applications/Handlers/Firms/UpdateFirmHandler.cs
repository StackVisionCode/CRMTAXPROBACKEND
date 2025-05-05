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
    public class UpdateFirmHandler : IRequestHandler<UpdateFirmCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateFirmHandler> _logger;

        public UpdateFirmHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateFirmHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateFirmCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var firm = await _dbContext.Firms.FirstOrDefaultAsync(x => x.Id == request.Firm.Id, cancellationToken);
                if (firm == null)
                {
                    return new ApiResponse<bool>(false, "Firm not found", false);
                }

                _mapper.Map(request.Firm, firm);
                _dbContext.Firms.Update(firm);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
                return new ApiResponse<bool>(result, result ? "Firm updated successfully" : "Failed to update firm", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating firm: {Message}", ex.Message);
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
    }
}
