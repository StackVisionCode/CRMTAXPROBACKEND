using AuthService.Domains.CustomPlans;
using AuthService.Domains.Modules;
using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Commands.CustomPlanCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomPlanHandlers;

/// <summary>
/// Handler para crear un nuevo CustomPlan
/// </summary>
public class CreateCustomPlanHandler
    : IRequestHandler<CreateCustomPlanCommand, ApiResponse<CustomPlanDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CreateCustomPlanHandler> _logger;

    public CreateCustomPlanHandler(
        ApplicationDbContext dbContext,
        ILogger<CreateCustomPlanHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomPlanDTO>> Handle(
        CreateCustomPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.CustomPlanData;

            // 1. Verificar que la Company existe y no tiene CustomPlan
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == dto.CompanyId
                select new
                {
                    Company = c,
                    HasCustomPlan = _dbContext.CustomPlans.Any(cp => cp.CompanyId == c.Id),
                };

            var companyData = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyData?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", dto.CompanyId);
                return new ApiResponse<CustomPlanDTO>(false, "Company not found", null!);
            }

            if (companyData.HasCustomPlan)
            {
                _logger.LogWarning("Company already has a CustomPlan: {CompanyId}", dto.CompanyId);
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "Company already has a CustomPlan",
                    null!
                );
            }

            // 2. Validar datos
            if (dto.Price < 0)
            {
                return new ApiResponse<CustomPlanDTO>(false, "Price cannot be negative", null!);
            }

            // 2.1. Validar UserLimit
            if (dto.UserLimit < 1)
            {
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "User limit must be at least 1",
                    null!
                );
            }

            // 3. Crear CustomPlan con UserLimit
            var customPlan = new CustomPlan
            {
                Id = Guid.NewGuid(),
                CompanyId = dto.CompanyId,
                Price = dto.Price,
                UserLimit = dto.UserLimit,
                IsActive = dto.IsActive,
                StartDate = dto.StartDate,
                RenewDate = dto.RenewDate,
                isRenewed = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.CustomPlans.AddAsync(customPlan, cancellationToken);

            // 4. Crear CustomModules si se proporcionan (sin cambios)
            if (dto.CustomModules?.Any() == true)
            {
                var moduleIds = dto.CustomModules.Select(cm => cm.ModuleId).ToList();

                // Verificar que todos los módulos existen
                var existingModules = await _dbContext
                    .Modules.Where(m => moduleIds.Contains(m.Id))
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                var missingModules = moduleIds.Except(existingModules).ToList();
                if (missingModules.Any())
                {
                    return new ApiResponse<CustomPlanDTO>(
                        false,
                        $"Modules not found: {string.Join(", ", missingModules)}",
                        null!
                    );
                }

                // Crear CustomModules
                foreach (var moduleDto in dto.CustomModules)
                {
                    var customModule = new CustomModule
                    {
                        Id = Guid.NewGuid(),
                        CustomPlanId = customPlan.Id,
                        ModuleId = moduleDto.ModuleId,
                        IsIncluded = moduleDto.IsIncluded,
                        CreatedAt = DateTime.UtcNow,
                    };

                    await _dbContext.CustomModules.AddAsync(customModule, cancellationToken);
                }
            }

            // 5. Actualizar Company con el CustomPlanId (sin cambios)
            companyData.Company.CustomPlanId = customPlan.Id;
            companyData.Company.UpdatedAt = DateTime.UtcNow;

            // 6. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomPlanDTO>(false, "Failed to create CustomPlan", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 7. Obtener CustomPlan completo para respuesta
            var createdPlanQuery =
                from cp in _dbContext.CustomPlans
                join c in _dbContext.Companies on cp.CompanyId equals c.Id
                where cp.Id == customPlan.Id
                select new
                {
                    CustomPlan = cp,
                    Company = c,
                    CustomModules = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id
                        select new { CustomModule = cm, Module = m }
                    ).ToList(),
                };

            var planData = await createdPlanQuery.FirstOrDefaultAsync(cancellationToken);
            if (planData?.CustomPlan == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "Failed to retrieve created CustomPlan",
                    null!
                );
            }

            // 8. Mapear con UserLimit
            var customPlanDto = new CustomPlanDTO
            {
                Id = planData.CustomPlan.Id,
                CompanyId = planData.CustomPlan.CompanyId,
                Price = planData.CustomPlan.Price,
                UserLimit = planData.CustomPlan.UserLimit,
                IsActive = planData.CustomPlan.IsActive,
                StartDate = planData.CustomPlan.StartDate,
                isRenewed = planData.CustomPlan.isRenewed,
                RenewedDate = planData.CustomPlan.RenewedDate,
                RenewDate = planData.CustomPlan.RenewDate,
                CompanyName = planData.Company.IsCompany
                    ? planData.Company.CompanyName
                    : planData.Company.FullName,
                CompanyDomain = planData.Company.Domain,
                CustomModules = planData
                    .CustomModules.Select(cm => new CustomModuleDTO
                    {
                        Id = cm.CustomModule.Id,
                        CustomPlanId = cm.CustomModule.CustomPlanId,
                        ModuleId = cm.CustomModule.ModuleId,
                        IsIncluded = cm.CustomModule.IsIncluded,
                        ModuleName = cm.Module.Name,
                        ModuleDescription = cm.Module.Description,
                        ModuleUrl = cm.Module.Url,
                    })
                    .ToList(),
                AdditionalModuleNames = planData
                    .CustomModules.Where(cm => cm.CustomModule.IsIncluded)
                    .Select(cm => cm.Module.Name)
                    .ToList(),
            };

            _logger.LogInformation(
                "CustomPlan created successfully: {CustomPlanId}, UserLimit: {UserLimit}",
                customPlan.Id,
                customPlan.UserLimit
            );

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan created successfully",
                customPlanDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating CustomPlan");
            return new ApiResponse<CustomPlanDTO>(false, "Error creating CustomPlan", null!);
        }
    }
}
