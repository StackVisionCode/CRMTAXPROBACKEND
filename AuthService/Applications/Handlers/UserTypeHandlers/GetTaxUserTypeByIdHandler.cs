using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserTypeQueries;
using UserDTOS;

namespace Handlers.UserTypeHandlers;

public class GetTaxUserTypeByIdHandler : IRequestHandler<GetTaxUserByIdQuery, ApiResponse<TaxUserTypeDTO>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetTaxUserTypeByIdHandler> _logger;
  public GetTaxUserTypeByIdHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetTaxUserTypeByIdHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<TaxUserTypeDTO>> Handle(GetTaxUserByIdQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var userType = await _dbContext.TaxUserTypes
                              .AsNoTracking()
                              .FirstOrDefaultAsync(u => u.Id == request.UsertTaxTypeId, cancellationToken);

      if (userType is null)
        return new(false, "User type not found");

      var userTypeDto = _mapper.Map<TaxUserTypeDTO>(userType);

      return new ApiResponse<TaxUserTypeDTO>(true, "Ok", userTypeDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching user type {Id}", request.UsertTaxTypeId);
      return new(false, ex.Message);
    }
  }
}