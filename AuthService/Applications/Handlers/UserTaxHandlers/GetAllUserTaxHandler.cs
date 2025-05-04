using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;
using UserDTOS;

namespace Handlers.UserTaxHandlers;

public class GetAllUserTaxHandler : IRequestHandler<GetAllUserQuery, ApiResponse<List<UserDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateUserTaxHandler> _logger;
    public GetAllUserTaxHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateUserTaxHandler> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }
    public async Task<ApiResponse<List<UserDTO>>> Handle(GetAllUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _dbContext.TaxUsers.ToListAsync(cancellationToken);
            _logger.LogInformation("User tax getting successfully: {UserTax}", users);
            if (users == null || !users.Any())
            {
                return new ApiResponse<List<UserDTO>>(false, "No user tax found", null!);
            }
            var userDTOs = _mapper.Map<List<UserDTO>>(users);
            return new ApiResponse<List<UserDTO>>(true, "User tax retrieved successfully", userDTOs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user tax: {Message}", ex.Message);
            return new ApiResponse<List<UserDTO>>(false, ex.Message, null!);
        }
    }
}
