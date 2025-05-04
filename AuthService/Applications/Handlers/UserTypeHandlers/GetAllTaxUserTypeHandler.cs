using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserTypeQueries;
using UserDTOS;

namespace Handlers.UserTypeHandlers;

public class GetAllTaxUserTypeHandler : IRequestHandler<GetAllTaxUserTypeQuery, ApiResponse<List<TaxUserTypeDTO>>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetAllTaxUserTypeHandler> _logger;
  public GetAllTaxUserTypeHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllTaxUserTypeHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<List<TaxUserTypeDTO>>> Handle(GetAllTaxUserTypeQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var userTypes = await _dbContext.TaxUserTypes.ToListAsync(cancellationToken);
      if (userTypes == null || !userTypes.Any())
      {
        return new ApiResponse<List<TaxUserTypeDTO>>(false, "No user types found", null!);
      }

      var userTypeDtos = _mapper.Map<List<TaxUserTypeDTO>>(userTypes);
      _logger.LogInformation("User types retrieved successfully: {UserTypes}", userTypeDtos);
      return new ApiResponse<List<TaxUserTypeDTO>>(true, "User types retrieved successfully", userTypeDtos);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving user types: {Message}", ex.Message);
      return new ApiResponse<List<TaxUserTypeDTO>>(false, ex.Message, null!);
    }
  }
}