
using AuthService.Domains.Users;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.UserTaxHandlers;

public class CreateUserTaxHandler : IRequestHandler<CreateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateUserTaxHandler> _logger;
    public CreateUserTaxHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateUserTaxHandler> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(CreateTaxUserCommands request, CancellationToken cancellationToken)
    {
        try
        {
           var userTax = _mapper.Map<TaxUser>(request.Usertax);
           userTax.CreatedAt = DateTime.UtcNow;
           userTax.IsActive=true;
            await _dbContext.TaxUsers.AddAsync(userTax, cancellationToken);
          var result= await _dbContext.SaveChangesAsync(cancellationToken)>0?true:false;
          _logger.LogInformation("User tax created successfully: {UserTax}", userTax);
            return new ApiResponse<bool>(result, result ? "User tax created successfully" : "Failed to create user tax", result);
         
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user tax: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
            
        }
    }


}