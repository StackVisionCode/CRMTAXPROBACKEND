using Commands.CompanyUserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyUserHandlers;

public class DeleteCompanyUserHandler : IRequestHandler<DeleteCompanyUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteCompanyUserHandler> _logger;

    public DeleteCompanyUserHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteCompanyUserHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteCompanyUserCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyUser = await _dbContext
                .CompanyUsers.Include(cu => cu.CompanyUserProfile)
                .Include(cu => cu.CompanyUserSessions)
                .Include(cu => cu.CompanyUserRoles)
                .FirstOrDefaultAsync(cu => cu.Id == request.CompanyUserId, cancellationToken);

            if (companyUser is null)
            {
                _logger.LogWarning(
                    "Company user not found: {CompanyUserId}",
                    request.CompanyUserId
                );
                return new ApiResponse<bool>(false, "Company user not found", false);
            }

            // 1. Revocar todas las sesiones activas
            foreach (var session in companyUser.CompanyUserSessions.Where(s => !s.IsRevoke))
            {
                session.IsRevoke = true;
                session.UpdatedAt = DateTime.UtcNow;
            }

            // 2. Soft delete del usuario (usando DeleteAt)
            companyUser.DeleteAt = DateTime.UtcNow;
            companyUser.IsActive = false;
            companyUser.UpdatedAt = DateTime.UtcNow;

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            _logger.LogInformation(
                "Company user deleted successfully: {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<bool>(true, "Company user deleted successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company user: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
