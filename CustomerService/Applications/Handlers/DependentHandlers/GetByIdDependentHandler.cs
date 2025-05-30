using AutoMapper;
using Common;
using CustomerService.DTOs.DependentDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.DependentQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.DependentHandlers;

public class GetByIdDependentHandler : IRequestHandler<GetByIdDependentQueries, ApiResponse<ReadDependentDTO>>
{
  private readonly ILogger<GetByIdDependentHandler> _logger;
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  public GetByIdDependentHandler(ILogger<GetByIdDependentHandler> logger, ApplicationDbContext dbContext, IMapper mapper)
  {
    _logger = logger;
    _dbContext = dbContext;
    _mapper = mapper;
  }
  public async Task<ApiResponse<ReadDependentDTO>> Handle(GetByIdDependentQueries request, CancellationToken cancellationToken)
  {
    try
    {
      var result = await (
        from dependent in _dbContext.Dependents
        join customer in _dbContext.Customers on dependent.CustomerId equals customer.Id
        join relationship in _dbContext.Relationships on dependent.RelationshipId equals relationship.Id

        select new ReadDependentDTO
        {
          Id = dependent.Id,
          FullName = dependent.FullName,
          DateOfBirth = dependent.DateOfBirth,
          Customer = customer.FirstName + " " + customer.LastName,
          Relationship = relationship.Name
        }
      ).FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

      if (result is null)
      {
        _logger.LogInformation("No dependents found.");
        return new ApiResponse<ReadDependentDTO>(false, "No dependents found.", null!);
      }

      var dependentDTO = _mapper.Map<ReadDependentDTO>(result);
      _logger.LogInformation("Dependent retrieved successfully: {Dependent}", dependentDTO);
      return new ApiResponse<ReadDependentDTO>(true, "Dependent retrieved successfully.", result);
    }
    catch (Exception e)
    {
      _logger.LogError("Error retrieving dependent: {Message}", e.Message);
      return new ApiResponse<ReadDependentDTO>(false, e.Message, null!);
    }
  }
}