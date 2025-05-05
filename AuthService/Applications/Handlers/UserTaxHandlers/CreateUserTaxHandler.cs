
using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserDTOS;

namespace Handlers.UserTaxHandlers;

public class CreateUserTaxHandler : IRequestHandler<CreateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateUserTaxHandler> _logger;
    private readonly IPasswordHash _passwordHash;
    public CreateUserTaxHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateUserTaxHandler> logger, IPasswordHash passwordHash)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _passwordHash = passwordHash;
    }

    public async Task<ApiResponse<bool>> Handle(CreateTaxUserCommands request, CancellationToken cancellationToken)
    {
        try
        {
            var userExists = await Exists(request.Usertax);
            if (userExists)
            {
                _logger.LogWarning("User already exists: {Email}", request.Usertax.Email);
                return new ApiResponse<bool>(false, "User already exists", false);
            }
            
            request.Usertax.Password = _passwordHash.HashPassword(request.Usertax.Password);

            var userTax = _mapper.Map<TaxUser>(request.Usertax);
            userTax.Confirm = false;
            userTax.IsActive = true;
            userTax.CreatedAt = DateTime.UtcNow;
            await _dbContext.TaxUsers.AddAsync(userTax, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation("User tax created successfully: {UserTax}", userTax);
            return new ApiResponse<bool>(result, result ? "User tax created successfully" : "Failed to create user tax", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user tax: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    private async Task<bool> Exists(NewUserDTO userDTO)
    {
        try
        {
            return await _dbContext.TaxUsers.FirstOrDefaultAsync(a => a.Email == userDTO.Email) != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if user exists.");
            throw new Exception("Error occurred while checking if user exists.");
        }
    }
}