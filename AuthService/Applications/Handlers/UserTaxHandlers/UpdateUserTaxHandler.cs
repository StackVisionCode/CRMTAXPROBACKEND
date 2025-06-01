using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

public class UpdateUserTaxHandler : IRequestHandler<UpdateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserTaxHandler> _logger;
    private readonly IPasswordHash _passwordHash;

    public UpdateUserTaxHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdateUserTaxHandler> logger,
        IPasswordHash passwordHash
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _passwordHash = passwordHash;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateTaxUserCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var userTax = await _dbContext
                .TaxUsers.Include(u => u.TaxUserProfile)
                .FirstOrDefaultAsync(u => u.Id == request.UserTax.Id, cancellationToken);

            if (userTax == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserTax.Id);
                return new ApiResponse<bool>(false, "User not found", false);
            }

            // Verificar si el email ya existe en otro usuario
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == request.UserTax.Email && u.Id != request.UserTax.Id,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already exists: {Email}", request.UserTax.Email);
                return new ApiResponse<bool>(false, "Email already exists", false);
            }

            // Actualizar datos del usuario
            userTax.Email = request.UserTax.Email;
            userTax.Domain = request.UserTax.Domain;
            userTax.IsActive = request.UserTax.IsActive ?? userTax.IsActive;
            userTax.UpdatedAt = DateTime.UtcNow;
            userTax.Password = _passwordHash.HashPassword(userTax.Password);

            // Actualizar perfil del usuario
            if (userTax.TaxUserProfile != null)
            {
                userTax.TaxUserProfile.Name = request.UserTax.Name;
                userTax.TaxUserProfile.LastName = request.UserTax.LastName;
                userTax.TaxUserProfile.PhoneNumber = request.UserTax.Phone;
                userTax.TaxUserProfile.Address = request.UserTax.Address;
                userTax.TaxUserProfile.PhotoUrl = request.UserTax.PhotoUrl;
                userTax.TaxUserProfile.UpdatedAt = DateTime.UtcNow;
            }

            _mapper.Map(request.UserTax, userTax);
            _dbContext.TaxUsers.Update(userTax);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("User updated successfully: {UserId}", userTax.Id);
                return new ApiResponse<bool>(true, "User updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update user: {UserId}", request.UserTax.Id);
                return new ApiResponse<bool>(false, "Failed to update user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating user: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
