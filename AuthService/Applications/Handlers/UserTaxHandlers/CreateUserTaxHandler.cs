using Applications.Common;
using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

namespace Handlers.UserTaxHandlers;

public class CreateUserTaxHandler : IRequestHandler<CreateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateUserTaxHandler> _logger;
    private readonly IPasswordHash _passwordHash;
    IConfirmTokenService _confirmTokenService;
    private readonly LinkBuilder _linkBuilder;
    private readonly IEventBus _eventBus;

    public CreateUserTaxHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreateUserTaxHandler> logger,
        IPasswordHash passwordHash,
        IEventBus eventBus,
        IConfirmTokenService confirmTokenService,
        LinkBuilder linkBuilder
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _passwordHash = passwordHash;
        _eventBus = eventBus;
        _confirmTokenService = confirmTokenService;
        _linkBuilder = linkBuilder;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateTaxUserCommands request,
        CancellationToken cancellationToken
    )
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
            userTax.IsActive = false;
            userTax.CreatedAt = DateTime.UtcNow;

            // 1.1)  Rol “User”
            var role = await _dbContext
                .Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == "TaxPreparer", cancellationToken);
            if (role is null)
                return new(false, "TaxPreparer role not found", false);

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
                UpdatedAt = DateTime.UtcNow,
            };

            // 1.3)  UserRole
            await _dbContext.UserRoles.AddAsync(
                new UserRole
                {
                    Id = Guid.NewGuid(),
                    TaxUserId = userTax.Id,
                    RoleId = role.Id,
                    CreatedAt = DateTime.UtcNow,
                },
                cancellationToken
            );

            // Generar y asignar el token de confirmación
            var (token, expiration) = _confirmTokenService.Generate(userTax.Id, userTax.Email);
            userTax.ConfirmToken = token;

            // Agregar solo el TaxUser - EF agregará automáticamente el TaxUserProfile
            await _dbContext.TaxUsers.AddAsync(userTax, cancellationToken);

            // Guardar ambas entidades en una sola operación
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);

                string link = _linkBuilder.BuildConfirmationLink(
                    request.Origin,
                    userTax.Email,
                    token
                );

                _logger.LogInformation("User tax created successfully: {UserId}", userTax.Id);

                // Notificar que se creo el usuario a CloudShield para crear nube.
                _eventBus.Publish(
                    new AccountRegisteredEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        userTax.Id,
                        userTax.Email,
                        request.Usertax.Name,
                        request.Usertax.LastName,
                        request.Usertax.Phone,
                        false,
                        null,
                        null,
                        null,
                        request.Usertax.Domain
                    )
                );

                // Notificar que se creo el usuario para enviar link de confirmación.
                _eventBus.Publish(
                    new AccountConfirmationLinkEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        userTax.Id,
                        userTax.Email,
                        $"{request.Usertax.Name} {request.Usertax.LastName}".Trim(),
                        link,
                        expiration,
                        false
                    )
                );

                _logger.LogInformation(
                    "Event published for user tax creation: {UserId}",
                    userTax.Id
                );
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
            return await _dbContext.TaxUsers.FirstOrDefaultAsync(a => a.Email == userDTO.Email)
                != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if user exists.");
            throw new Exception("Error occurred while checking if user exists.");
        }
    }
}
