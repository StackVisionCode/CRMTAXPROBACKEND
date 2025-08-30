using Commands.CustomPlanCommands;
using Common;
using Domains;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomPlanHandlers;

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

            // 1. Verificar que NO existe CustomPlan para esta Company
            var existingPlan = await _dbContext.CustomPlans.FirstOrDefaultAsync(
                cp => cp.CompanyId == dto.CompanyId,
                cancellationToken
            );

            if (existingPlan != null)
            {
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "Company already has a CustomPlan",
                    null!
                );
            }

            // 2. Buscar Service por ServiceLevel
            var service = await _dbContext
                .Services.Include(s => s.Modules)
                .FirstOrDefaultAsync(
                    s => s.ServiceLevel == dto.ServiceLevel && s.IsActive,
                    cancellationToken
                );

            if (service == null)
            {
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    $"Service not found for ServiceLevel {dto.ServiceLevel}",
                    null!
                );
            }

            // 3. Crear CustomPlan basado en Service
            var customPlan = new CustomPlan
            {
                Id = Guid.NewGuid(),
                CompanyId = dto.CompanyId,
                Price = service.Price, // Obtener del Service
                UserLimit = service.UserLimit, // Obtener del Service
                IsActive = dto.IsActive,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                RenewDate = dto.RenewDate,
                IsRenewed = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.CustomPlans.AddAsync(customPlan, cancellationToken);

            // 4. Copiar módulos del Service al CustomPlan
            if (service.Modules?.Any() == true)
            {
                foreach (var module in service.Modules)
                {
                    var customModule = new CustomModule
                    {
                        Id = Guid.NewGuid(),
                        CustomPlanId = customPlan.Id,
                        ModuleId = module.Id,
                        IsIncluded = true, // Por defecto incluidos
                        CreatedAt = DateTime.UtcNow,
                    };
                    await _dbContext.CustomModules.AddAsync(customModule, cancellationToken);
                }
            }

            // 5. Agregar módulos personalizados adicionales si se proporcionan
            if (dto.CustomModules?.Any() == true)
            {
                var moduleIds = dto.CustomModules.Select(cm => cm.ModuleId).ToList();

                var existingModules = await _dbContext
                    .Modules.Where(m => moduleIds.Contains(m.Id))
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                var missingModules = moduleIds.Except(existingModules).ToList();
                if (missingModules.Any())
                {
                    return new ApiResponse<CustomPlanDTO>(
                        false,
                        $"Custom modules not found: {string.Join(", ", missingModules)}",
                        null!
                    );
                }

                foreach (var moduleDto in dto.CustomModules)
                {
                    // Verificar que no se duplique con módulos del service
                    var existsInBase = await _dbContext.CustomModules.AnyAsync(
                        cm => cm.CustomPlanId == customPlan.Id && cm.ModuleId == moduleDto.ModuleId,
                        cancellationToken
                    );

                    if (!existsInBase)
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
            }

            // 6. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomPlanDTO>(false, "Failed to create CustomPlan", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 7. Construir respuesta
            var response = new CustomPlanDTO
            {
                Id = customPlan.Id,
                CompanyId = customPlan.CompanyId,
                Price = customPlan.Price,
                UserLimit = customPlan.UserLimit,
                IsActive = customPlan.IsActive,
                StartDate = customPlan.StartDate,
                RenewDate = customPlan.RenewDate,
                isRenewed = customPlan.IsRenewed,
                RenewedDate = customPlan.RenewedDate,
                AdditionalModuleNames =
                    service.Modules?.Select(m => m.Name).ToList() ?? new List<string>(),
            };

            _logger.LogInformation(
                "CustomPlan created: {CustomPlanId} for Company: {CompanyId} with ServiceLevel: {ServiceLevel}",
                customPlan.Id,
                customPlan.CompanyId,
                dto.ServiceLevel
            );

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan created successfully",
                response
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error creating CustomPlan for ServiceLevel {ServiceLevel}",
                request.CustomPlanData.ServiceLevel
            );
            return new ApiResponse<CustomPlanDTO>(false, "Error creating CustomPlan", null!);
        }
    }
}
