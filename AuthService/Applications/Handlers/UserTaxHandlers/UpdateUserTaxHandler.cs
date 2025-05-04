using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.UserTaxHandlers;

public class UpdateUserTaxHandler : IRequestHandler<UpdateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserTaxHandler> _logger;
    public UpdateUserTaxHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateUserTaxHandler> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

public async Task<ApiResponse<bool>> Handle(UpdateTaxUserCommands request, CancellationToken cancellationToken)
    {
        try
        {
            var userTax = await _dbContext.TaxUsers.FindAsync(new object[] { request.Usertax.Id }, cancellationToken);
            if (userTax == null)
            {
                return new ApiResponse<bool>(false, "User tax not found", false);
            }

            _mapper.Map(request.Usertax, userTax);
            userTax.UpdatedAt = DateTime.UtcNow;
            _dbContext.TaxUsers.Update(userTax);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
            _logger.LogInformation("User tax updated successfully: {UserTax}", userTax);
            return new ApiResponse<bool>(result, result ? "User tax updated successfully" : "Failed to update user tax", result);
        }
        catch (Exception ex)
        {
            
            _logger.LogError(ex, "Error updating user tax: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
