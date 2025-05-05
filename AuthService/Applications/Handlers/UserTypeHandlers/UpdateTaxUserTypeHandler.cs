using AutoMapper;
using Commands.UserTypeCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.UserTypeHandlers;

public class UpdateTaxUserTypeHandler : IRequestHandler<UpdateTaxUserTypeCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<UpdateTaxUserTypeHandler> _logger;
  public UpdateTaxUserTypeHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateTaxUserTypeHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<bool>> Handle(UpdateTaxUserTypeCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var userType = await _dbContext.TaxUserTypes.FindAsync(new object[] { request.UserType.Id }, cancellationToken);
      if (userType == null)
      {
        return new ApiResponse<bool>(false, "User type not found", false);
      }

      _mapper.Map(request.UserType, userType);
      userType.UpdatedAt = DateTime.UtcNow;
      _dbContext.TaxUserTypes.Update(userType);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
      _logger.LogInformation("User type updated successfully: {UserType}", userType);
      return new ApiResponse<bool>(result, result ? "User type updated successfully" : "Failed to update user type", result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating user type: {Message}", ex.Message);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }
}    