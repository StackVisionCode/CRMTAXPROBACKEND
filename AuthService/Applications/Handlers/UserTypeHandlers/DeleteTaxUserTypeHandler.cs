using Commands.UserTypeCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.UserTypeHandlers;

public class DeleteTaxUserTypeHandler : IRequestHandler<DeleteTaxUserTypeCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly ILogger<DeleteTaxUserTypeHandler> _logger;
  public DeleteTaxUserTypeHandler(ApplicationDbContext dbContext, ILogger<DeleteTaxUserTypeHandler> logger)
  {
    _dbContext = dbContext;
    _logger = logger;
  }
  public Task<ApiResponse<bool>> Handle(DeleteTaxUserTypeCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var userType = _dbContext.TaxUserTypes.FirstOrDefault(x => x.Id == request.UserTypeId);
      if (userType == null)
      {
        return Task.FromResult(new ApiResponse<bool>(false, "User type not found", false));
      }
      _dbContext.TaxUserTypes.Remove(userType);
      var result = _dbContext.SaveChanges() > 0 ? true : false;
      _logger.LogInformation("User type deleted successfully: {UserTypeId}", request.UserTypeId);
      return Task.FromResult(new ApiResponse<bool>(result, result ? "User type deleted successfully" : "Failed to delete user type", result));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting user type: {UserType}", request.UserTypeId);
      return Task.FromResult(new ApiResponse<bool>(false, "An error occurred while deleting the user type", false));
    }
  }
}    