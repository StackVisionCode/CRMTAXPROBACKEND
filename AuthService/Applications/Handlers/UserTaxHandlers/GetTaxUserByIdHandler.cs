using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserTaxHandlers;

public class GetTaxUserByIdHandler : IRequestHandler<GetTaxUserByIdQuery, ApiResponse<UserGetDTO>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetTaxUserByIdHandler> _logger;
  public GetTaxUserByIdHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetTaxUserByIdHandler> logger)
  {
    _dbContext = dbContext;
    _logger = logger;
    _mapper = mapper;
  }
  public async Task<ApiResponse<UserGetDTO>> Handle(GetTaxUserByIdQuery request, CancellationToken cancellationToken)
  {
    try
        {
            var user = await _dbContext.TaxUsers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (user is null)
                return new(false, "User not found");

            var dto = _mapper.Map<UserGetDTO>(user);

            return new ApiResponse<UserGetDTO>(true, "Ok", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {Id}", request.Id);
            return new(false, ex.Message);
        }
  }
}