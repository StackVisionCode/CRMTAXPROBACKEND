using Commands.ServiceCommands;
using Common;
using DTOs.ServiceDTOs;
using Infrastructure.Context;
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
        try
        {
            var dto = request.ServiceData;

            var service = await _dbContext.Services.FirstOrDefaultAsync(
                s => s.Id == dto.Id,
                cancellationToken
            );

            if (service == null)
            {
                return new ApiResponse<ServiceDTO>(false, "Service not found", null!);
            }

            // Verificar nombre único
            var nameExists = await _dbContext.Services.AnyAsync(
                s => s.Name == dto.Name && s.Id != dto.Id,
                cancellationToken
            );

            if (nameExists)
            {
                return new ApiResponse<ServiceDTO>(false, "Service name already exists", null!);
            }

            // Validaciones básicas
            if (dto.Price < 0)
                return new ApiResponse<ServiceDTO>(false, "Price cannot be negative", null!);

            if (dto.UserLimit < 1)
                return new ApiResponse<ServiceDTO>(false, "User limit must be at least 1", null!);

            if (string.IsNullOrWhiteSpace(dto.Title))
                return new ApiResponse<ServiceDTO>(false, "Title is required", null!);

            if (dto.Features == null || !dto.Features.Any())
                return new ApiResponse<ServiceDTO>(
                    false,
                    "At least one feature is required",
                    null!
                );

            // En SubscriptionsService no validamos impacto en usuarios, AuthService debe manejar eso

            // Actualizar Service
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

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                return new ApiResponse<ServiceDTO>(false, "Failed to update Service", null!);
            }

            // Construir respuesta simple
            var serviceDto = new ServiceDTO
            {
                Id = service.Id,
                Name = service.Name,
                Title = service.Title,
                Description = service.Description,
                Features = service.Features,
                Price = service.Price,
                UserLimit = service.UserLimit,
                IsActive = service.IsActive,
                CreatedAt = service.CreatedAt,
                ModuleNames = new List<string>(),
                ModuleIds = new List<Guid>(),
            };

            _logger.LogInformation("Service updated successfully: {ServiceId}", service.Id);
            return new ApiResponse<ServiceDTO>(true, "Service updated successfully", serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Service: {ServiceId}", request.ServiceData.Id);
            return new ApiResponse<ServiceDTO>(false, "Error updating Service", null!);
        }
    }
}
