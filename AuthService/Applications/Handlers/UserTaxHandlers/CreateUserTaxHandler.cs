
using AuthService.Domains.Roles;
using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

public class CreateUserTaxHandler : IRequestHandler<CreateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateUserTaxHandler> _logger;
    private readonly IPasswordHash _passwordHash;

    public CreateUserTaxHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateUserTaxHandler> logger, IPasswordHash passwordHash)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _passwordHash = passwordHash;
    }

    public async Task<ApiResponse<bool>> Handle(CreateTaxUserCommands request, CancellationToken cancellationToken)
    {
        // Usar transacción para asegurar atomicidad
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var userExists = await Exists(request.Usertax);
            if (userExists)
            {
                _logger.LogWarning("User already exists: {Email}", request.Usertax.Email);
                return new ApiResponse<bool>(false, "User already exists", false);
            }

            request.Usertax.Password = _passwordHash.HashPassword(request.Usertax.Password);
            request.Usertax.Id = Guid.NewGuid();

            var userTax = _mapper.Map<TaxUser>(request.Usertax);
            userTax.CompanyId = null;
            userTax.Confirm = false;
            userTax.IsActive = true;
            userTax.CreatedAt = DateTime.UtcNow;
            var roleGuid = await GetAllRoles();
            userTax.RoleId = roleGuid?.Id ?? Guid.Empty;

            // Crear el perfil asociado
            userTax.TaxUserProfile = new TaxUserProfile
            {
                Id = Guid.NewGuid(),
                TaxUserId = userTax.Id,
                Name = request.Usertax.Name,
                LastName = request.Usertax.LastName,
                PhoneNumber = request.Usertax.Phone,
                Address = request.Usertax.Address,
                PhotoUrl = request.Usertax.PhotoUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Agregar solo el TaxUser - EF agregará automáticamente el TaxUserProfile
            await _dbContext.TaxUsers.AddAsync(userTax, cancellationToken);

            // Guardar ambas entidades en una sola operación
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("User tax created successfully: {UserId}", userTax.Id);
                return new ApiResponse<bool>(true, "User tax created successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to save user and profile to database");
                return new ApiResponse<bool>(false, "Failed to create user tax", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating user tax: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    private async Task<bool> Exists(NewUserDTO userDTO)
    {
        try
        {
            return await _dbContext.TaxUsers.FirstOrDefaultAsync(a => a.Email == userDTO.Email) != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if user exists.");
            throw new Exception("Error occurred while checking if user exists.");
        }
    }

    private async Task<Role> GetAllRoles()
    {
        var result = await _dbContext.Roles.AsNoTracking().Where(a => a.Name.Contains("user")).FirstAsync();
        if (result is null)
        {
            return null!;
        }
        return result;
    }
}