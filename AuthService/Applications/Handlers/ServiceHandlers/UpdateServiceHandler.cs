using AuthService.DTOs.ServiceDTOs;
using Commands.ServiceCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ServiceHandlers;

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, ApiResponse<ServiceDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateServiceHandler> _logger;

    public UpdateServiceHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateServiceHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ServiceDTO>> Handle(
        UpdateServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.ServiceData;

            // 1. Verificar que el Service existe
            var service = await _dbContext.Services.FirstOrDefaultAsync(
                s => s.Id == dto.Id,
                cancellationToken
            );

            if (service == null)
            {
                _logger.LogWarning("Service not found: {ServiceId}", dto.Id);
                return new ApiResponse<ServiceDTO>(false, "Service not found", null!);
            }

            // 2. Verificar que el nombre no existe en otro Service
            var nameExists = await _dbContext.Services.AnyAsync(
                s => s.Name == dto.Name && s.Id != dto.Id,
                cancellationToken
            );

            if (nameExists)
            {
                _logger.LogWarning("Service name already exists: {Name}", dto.Name);
                return new ApiResponse<ServiceDTO>(false, "Service name already exists", null!);
            }

            // 3. Validar datos
            if (dto.Price < 0)
            {
                return new ApiResponse<ServiceDTO>(false, "Price cannot be negative", null!);
            }

            if (dto.UserLimit < 1)
            {
                return new ApiResponse<ServiceDTO>(false, "User limit must be at least 1", null!);
            }

            // 3.1. Validar Title y Features
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return new ApiResponse<ServiceDTO>(false, "Title is required", null!);
            }

            if (dto.Features == null || !dto.Features.Any())
            {
                return new ApiResponse<ServiceDTO>(
                    false,
                    "At least one feature is required",
                    null!
                );
            }

            // 4. Verificar impacto en Companies existentes con TaxUsers (IMPORTANTE)
            if (dto.UserLimit < service.UserLimit)
            {
                var companiesAffectedQuery =
                    from c in _dbContext.Companies
                    join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                    join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                    join m in _dbContext.Modules on cm.ModuleId equals m.Id
                    where m.ServiceId == service.Id && cm.IsIncluded
                    let activeTaxUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    )
                    where activeTaxUsers > dto.UserLimit && cp.UserLimit <= service.UserLimit
                    select new
                    {
                        CompanyId = c.Id,
                        ActiveUsers = activeTaxUsers,
                        CustomUserLimit = cp.UserLimit,
                    };

                var affectedCompanies = await companiesAffectedQuery.ToListAsync(cancellationToken);
                if (affectedCompanies.Any())
                {
                    var companiesInfo = string.Join(
                        ", ",
                        affectedCompanies.Select(ac => $"{ac.CompanyId} ({ac.ActiveUsers} users)")
                    );

                    _logger.LogWarning(
                        "Cannot reduce user limit from {OldLimit} to {NewLimit}. Companies affected: {Companies}",
                        service.UserLimit,
                        dto.UserLimit,
                        companiesInfo
                    );

                    return new ApiResponse<ServiceDTO>(
                        false,
                        $"Cannot reduce user limit. {affectedCompanies.Count} companies would exceed the new limit of {dto.UserLimit} users.",
                        null!
                    );
                }
            }

            // 5. Actualizar Service con nuevos campos
            service.Name = dto.Name.Trim();
            service.Title = dto.Title.Trim();
            service.Description = dto.Description.Trim();
            service.Features = dto
                .Features.Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.Trim())
                .ToList();
            service.Price = dto.Price;
            service.UserLimit = dto.UserLimit;
            service.IsActive = dto.IsActive;
            service.UpdatedAt = DateTime.UtcNow;

            // 6. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<ServiceDTO>(false, "Failed to update Service", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 7. Obtener Service actualizado para respuesta
            var updatedServiceQuery =
                from s in _dbContext.Services
                where s.Id == service.Id
                select new
                {
                    Service = s,
                    ModuleNames = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id && m.IsActive
                        select m.Name
                    ).ToList(),
                    ModuleIds = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id && m.IsActive
                        select m.Id
                    ).ToList(),
                };

            var serviceData = await updatedServiceQuery.FirstOrDefaultAsync(cancellationToken);

            // 8. Mapear con nuevos campos
            var serviceDto = new ServiceDTO
            {
                Id = serviceData!.Service.Id,
                Name = serviceData.Service.Name,
                Title = serviceData.Service.Title,
                Description = serviceData.Service.Description,
                Features = serviceData.Service.Features,
                Price = serviceData.Service.Price,
                UserLimit = serviceData.Service.UserLimit,
                IsActive = serviceData.Service.IsActive,
                ModuleNames = serviceData.ModuleNames,
                ModuleIds = serviceData.ModuleIds,
                CreatedAt = serviceData.Service.CreatedAt,
            };

            _logger.LogInformation(
                "Service updated successfully: {ServiceId}, UserLimit: {NewLimit}",
                service.Id,
                dto.UserLimit
            );

            return new ApiResponse<ServiceDTO>(true, "Service updated successfully", serviceDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating Service: {ServiceId}", request.ServiceData.Id);
            return new ApiResponse<ServiceDTO>(false, "Error updating Service", null!);
        }
    }
}
