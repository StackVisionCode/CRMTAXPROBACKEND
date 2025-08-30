using Commands.ServiceCommands;
using Common;
using Domains;
using DTOs.ServiceDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ServiceHandlers;

/// Handler para crear un nuevo Service
public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, ApiResponse<ServiceDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CreateServiceHandler> _logger;

    public CreateServiceHandler(
        ApplicationDbContext dbContext,
        ILogger<CreateServiceHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ServiceDTO>> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.ServiceData;

            // 1. Verificar que el nombre no existe
            var nameExists = await _dbContext.Services.AnyAsync(
                s => s.Name == dto.Name,
                cancellationToken
            );

            if (nameExists)
            {
                _logger.LogWarning("Service name already exists: {Name}", dto.Name);
                return new ApiResponse<ServiceDTO>(false, "Service name already exists", null!);
            }

            // 2. Validar datos
            if (dto.Price < 0)
            {
                return new ApiResponse<ServiceDTO>(false, "Price cannot be negative", null!);
            }

            if (dto.UserLimit < 1)
            {
                return new ApiResponse<ServiceDTO>(false, "User limit must be at least 1", null!);
            }

            // 3. Validar Title y Features
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

            // 4. Crear Service con nuevos campos
            var service = new Service
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Features = dto
                    .Features.Where(f => !string.IsNullOrWhiteSpace(f))
                    .Select(f => f.Trim())
                    .ToList(),
                Price = dto.Price,
                UserLimit = dto.UserLimit,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.Services.AddAsync(service, cancellationToken);

            // 5. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<ServiceDTO>(false, "Failed to create Service", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 6. Obtener Service completo para respuesta
            var createdServiceQuery =
                from s in _dbContext.Services
                where s.Id == service.Id
                select new
                {
                    Service = s,
                    ModuleNames = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id
                        select m.Name
                    ).ToList(),
                    ModuleIds = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id
                        select m.Id
                    ).ToList(),
                };

            var serviceData = await createdServiceQuery.FirstOrDefaultAsync(cancellationToken);

            // 7. Mapear con nuevos campos
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

            _logger.LogInformation("Service created successfully: {ServiceId}", service.Id);

            return new ApiResponse<ServiceDTO>(true, "Service created successfully", serviceDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating Service");
            return new ApiResponse<ServiceDTO>(false, "Error creating Service", null!);
        }
    }
}
