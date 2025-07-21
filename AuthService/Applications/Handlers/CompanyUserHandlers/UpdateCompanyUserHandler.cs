using AuthService.Infraestructure.Services;
using Commands.CompanyUserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyUserHandlers;

public class UpdateCompanyUserHandler : IRequestHandler<UpdateCompanyUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCompanyUserHandler> _logger;
    private readonly IPasswordHash _passwordHash;

    public UpdateCompanyUserHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCompanyUserHandler> logger,
        IPasswordHash passwordHash
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _passwordHash = passwordHash;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateCompanyUserCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var companyUser = await _dbContext
                .CompanyUsers.Include(cu => cu.CompanyUserProfile)
                .FirstOrDefaultAsync(cu => cu.Id == request.CompanyUser.Id, cancellationToken);

            if (companyUser is null)
            {
                _logger.LogWarning(
                    "Company user not found: {CompanyUserId}",
                    request.CompanyUser.Id
                );
                return new ApiResponse<bool>(false, "Company user not found", false);
            }

            // Validar email único si se está cambiando
            if (
                !string.IsNullOrWhiteSpace(request.CompanyUser.Email)
                && request.CompanyUser.Email != companyUser.Email
            )
            {
                var emailExists = await _dbContext.CompanyUsers.AnyAsync(
                    cu => cu.Email == request.CompanyUser.Email && cu.Id != request.CompanyUser.Id,
                    cancellationToken
                );

                if (emailExists)
                    return new ApiResponse<bool>(false, "Email already exists", false);

                companyUser.Email = request.CompanyUser.Email;
            }

            // Actualizar campos del usuario
            if (!string.IsNullOrWhiteSpace(request.CompanyUser.Password))
            {
                companyUser.Password = _passwordHash.HashPassword(request.CompanyUser.Password);
            }

            if (request.CompanyUser.IsActive.HasValue)
            {
                companyUser.IsActive = request.CompanyUser.IsActive.Value;
            }

            companyUser.UpdatedAt = DateTime.UtcNow;

            // Actualizar perfil si existe
            if (companyUser.CompanyUserProfile != null)
            {
                if (!string.IsNullOrWhiteSpace(request.CompanyUser.Name))
                    companyUser.CompanyUserProfile.Name = request.CompanyUser.Name;

                if (!string.IsNullOrWhiteSpace(request.CompanyUser.LastName))
                    companyUser.CompanyUserProfile.LastName = request.CompanyUser.LastName;

                if (!string.IsNullOrWhiteSpace(request.CompanyUser.Phone))
                    companyUser.CompanyUserProfile.PhoneNumber = request.CompanyUser.Phone;

                if (request.CompanyUser.Address != null)
                    companyUser.CompanyUserProfile.Address = request.CompanyUser.Address;

                if (request.CompanyUser.PhotoUrl != null)
                    companyUser.CompanyUserProfile.PhotoUrl = request.CompanyUser.PhotoUrl;

                if (request.CompanyUser.Position != null)
                    companyUser.CompanyUserProfile.Position = request.CompanyUser.Position;

                companyUser.CompanyUserProfile.UpdatedAt = DateTime.UtcNow;
            }

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Company user updated successfully: {CompanyUserId}",
                    request.CompanyUser.Id
                );
                return new ApiResponse<bool>(true, "Company user updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to update company user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating company user: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
