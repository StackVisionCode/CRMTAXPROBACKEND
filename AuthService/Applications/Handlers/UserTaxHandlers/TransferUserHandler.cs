using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserHandlers;

public class TransferUserHandler : IRequestHandler<TransferUserCommand, ApiResponse<UserGetDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TransferUserHandler> _logger;
    private readonly IMapper _mapper;

    public TransferUserHandler(
        ApplicationDbContext dbContext,
        ILogger<TransferUserHandler> logger,
        IMapper mapper
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UserGetDTO>> Handle(
        TransferUserCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Buscar TaxUser origen
            var sourceUserQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                where u.Id == request.UserId
                select new { User = u, SourceCompany = c };

            var sourceData = await sourceUserQuery.FirstOrDefaultAsync(cancellationToken);
            if (sourceData?.User == null)
            {
                return new ApiResponse<UserGetDTO>(false, "User not found", null!);
            }

            // 2. Verificar company destino y sus límites
            var targetCompanyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.TargetCompanyId
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                };

            var targetData = await targetCompanyQuery.FirstOrDefaultAsync(cancellationToken);
            if (targetData?.Company == null)
            {
                return new ApiResponse<UserGetDTO>(false, "Target company not found", null!);
            }

            // 3. Verificar límites del plan destino (solo para Users regulares)
            if (
                !sourceData.User.IsOwner
                && // Los Owners no cuentan en límites normales
                targetData.CurrentActiveUserCount >= targetData.UserLimit
            )
            {
                return new ApiResponse<UserGetDTO>(
                    false,
                    $"Target company has reached its user limit ({targetData.UserLimit}). Current users: {targetData.CurrentActiveUserCount}",
                    null!
                );
            }

            var user = sourceData.User;

            // 4. Actualizar CompanyId
            user.CompanyId = request.TargetCompanyId;
            user.UpdatedAt = DateTime.UtcNow;

            // 5. Revocar sesiones existentes (cambio de company requiere re-login)
            var activeSessions = await _dbContext
                .Sessions.Where(s => s.TaxUserId == request.UserId && !s.IsRevoke)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.IsRevoke = true;
                session.UpdatedAt = DateTime.UtcNow;
            }

            // 6. Actualizar permisos personalizados según el nuevo plan
            await UpdatePermissionsForNewCompanyAsync(
                request.UserId,
                targetData.CustomPlan.Id,
                cancellationToken
            );

            // 7. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<UserGetDTO>(false, "Failed to transfer user", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 8. Obtener TaxUser actualizado para respuesta
            var transferredUser = await _dbContext
                .TaxUsers.Include(u => u.Address)
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);

            var userDto = _mapper.Map<UserGetDTO>(transferredUser);

            _logger.LogInformation(
                "User transferred: {UserId} from {SourceCompany} to {TargetCompany}. Target users: {Current}/{Limit}",
                request.UserId,
                sourceData.SourceCompany.CompanyName ?? sourceData.SourceCompany.FullName,
                targetData.Company.CompanyName ?? targetData.Company.FullName,
                targetData.CurrentActiveUserCount + 1,
                targetData.UserLimit
            );

            return new ApiResponse<UserGetDTO>(true, "User transferred successfully", userDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error transferring user: {UserId}", request.UserId);
            return new ApiResponse<UserGetDTO>(false, "Error transferring user", null!);
        }
    }

    private async Task UpdatePermissionsForNewCompanyAsync(
        Guid userId,
        Guid newCustomPlanId,
        CancellationToken ct
    )
    {
        try
        {
            // Eliminar permisos personalizados existentes
            var existingPermissions = await _dbContext
                .CompanyPermissions.Where(cp => cp.TaxUserId == userId)
                .ToListAsync(ct);

            if (existingPermissions.Any())
            {
                _dbContext.CompanyPermissions.RemoveRange(existingPermissions);
            }

            // Los permisos base vienen de los roles, no necesitamos crear nuevos CompanyPermissions
            // a menos que la nueva company tenga permisos personalizados específicos
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating permissions for transferred user {UserId}",
                userId
            );
        }
    }
}
