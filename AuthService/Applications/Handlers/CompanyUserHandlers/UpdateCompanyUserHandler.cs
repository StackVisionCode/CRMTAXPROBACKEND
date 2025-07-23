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
            // PASO 1: Verificar existencia del usuario SIN Include
            var existingUserData = await _dbContext
                .CompanyUsers.Where(cu => cu.Id == request.CompanyUser.Id)
                .Select(cu => new
                {
                    cu.Id,
                    cu.Email,
                    cu.CompanyId,
                    HasProfile = _dbContext.CompanyUserProfiles.Any(cup =>
                        cup.CompanyUserId == cu.Id
                    ),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingUserData is null)
            {
                _logger.LogWarning(
                    "Company user not found: {CompanyUserId}",
                    request.CompanyUser.Id
                );
                return new ApiResponse<bool>(false, "Company user not found", false);
            }

            // PASO 2: Validar email único si se está cambiando (SIN cargar entidad)
            if (
                !string.IsNullOrWhiteSpace(request.CompanyUser.Email)
                && request.CompanyUser.Email != existingUserData.Email
            )
            {
                var emailExists = await _dbContext
                    .CompanyUsers.Where(cu =>
                        cu.Email == request.CompanyUser.Email && cu.Id != request.CompanyUser.Id
                    )
                    .AnyAsync(cancellationToken);

                if (emailExists)
                    return new ApiResponse<bool>(false, "Email already exists", false);
            }

            var updatedRows = 0;

            // PASO 3: Actualizar CompanyUser usando ExecuteUpdateAsync (SIMPLIFICADO)
            var hasUserUpdates =
                !string.IsNullOrWhiteSpace(request.CompanyUser.Email)
                || !string.IsNullOrWhiteSpace(request.CompanyUser.Password)
                || request.CompanyUser.IsActive.HasValue;

            if (hasUserUpdates)
            {
                // Preparar valores para evitar problemas de nullability
                var emailToUpdate = !string.IsNullOrWhiteSpace(request.CompanyUser.Email)
                    ? request.CompanyUser.Email
                    : existingUserData.Email;

                var passwordToUpdate = !string.IsNullOrWhiteSpace(request.CompanyUser.Password)
                    ? _passwordHash.HashPassword(request.CompanyUser.Password)
                    : null; // Se manejará condicionalmente

                var isActiveToUpdate = request.CompanyUser.IsActive ?? true;

                if (!string.IsNullOrWhiteSpace(request.CompanyUser.Password))
                {
                    // Si hay cambio de password, actualizar todo incluido el password
                    updatedRows += await _dbContext
                        .CompanyUsers.Where(cu => cu.Id == request.CompanyUser.Id)
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(u => u.Email, emailToUpdate)
                                    .SetProperty(u => u.Password, passwordToUpdate!)
                                    .SetProperty(u => u.IsActive, isActiveToUpdate)
                                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                            cancellationToken
                        );
                }
                else
                {
                    // Si NO hay cambio de password, actualizar solo email e isActive
                    updatedRows += await _dbContext
                        .CompanyUsers.Where(cu => cu.Id == request.CompanyUser.Id)
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(u => u.Email, emailToUpdate)
                                    .SetProperty(u => u.IsActive, isActiveToUpdate)
                                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                            cancellationToken
                        );
                }
            }

            // PASO 4: Actualizar CompanyUserProfile usando ExecuteUpdateAsync (si existe y hay cambios)
            if (existingUserData.HasProfile && HasProfileUpdates(request.CompanyUser))
            {
                updatedRows += await _dbContext
                    .CompanyUserProfiles.Where(cup => cup.CompanyUserId == request.CompanyUser.Id)
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(p => p.Name, request.CompanyUser.Name ?? string.Empty)
                                .SetProperty(
                                    p => p.LastName,
                                    request.CompanyUser.LastName ?? string.Empty
                                )
                                .SetProperty(
                                    p => p.PhoneNumber,
                                    request.CompanyUser.Phone ?? string.Empty
                                )
                                .SetProperty(p => p.Address, request.CompanyUser.Address)
                                .SetProperty(p => p.PhotoUrl, request.CompanyUser.PhotoUrl)
                                .SetProperty(p => p.Position, request.CompanyUser.Position)
                                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                        cancellationToken
                    );
            }

            var success = updatedRows > 0;

            if (success)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Company user updated successfully: {CompanyUserId}. Rows affected: {RowsAffected}",
                    request.CompanyUser.Id,
                    updatedRows
                );
                return new ApiResponse<bool>(true, "Company user updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(
                    false,
                    "No changes were made to the company user",
                    false
                );
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error updating company user {CompanyUserId}: {Message}",
                request.CompanyUser.Id,
                ex.Message
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    private static bool HasProfileUpdates(dynamic companyUser)
    {
        return !string.IsNullOrWhiteSpace(companyUser.Name)
            || !string.IsNullOrWhiteSpace(companyUser.LastName)
            || !string.IsNullOrWhiteSpace(companyUser.Phone)
            || companyUser.Address != null
            || companyUser.PhotoUrl != null
            || companyUser.Position != null;
    }
}
