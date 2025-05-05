using AutoMapper;
using Commands.UserTypeCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Users;

namespace Handlers.UserTypeHandlers;

public class CreateTaxUserTypeHandler : IRequestHandler<CreateTaxUserTypeCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<CreateTaxUserTypeHandler> _logger;
  public CreateTaxUserTypeHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateTaxUserTypeHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<bool>> Handle(CreateTaxUserTypeCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var userType = _mapper.Map<TaxUserType>(request.Typeuser);
      userType.CreatedAt = DateTime.UtcNow;
      await _dbContext.TaxUserTypes.AddAsync(userType, cancellationToken);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
      _logger.LogInformation("User type created successfully: {UserType}", userType);
      return new ApiResponse<bool>(result, result ? "User type created successfully" : "Failed to create user type", result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating user type: {Message}", ex.Message);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }
}