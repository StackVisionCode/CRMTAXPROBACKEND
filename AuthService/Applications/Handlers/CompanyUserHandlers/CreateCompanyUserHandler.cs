using Applications.Common;
using AuthService.Domains.CompanyUsers;
using AuthService.Domains.Roles;
using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.CompanyUserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

namespace Handlers.CompanyUserHandlers;

public class CreateCompanyUserHandler : IRequestHandler<CreateCompanyUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCompanyUserHandler> _logger;
    private readonly IPasswordHash _passwordHash;
    private readonly IConfirmTokenService _confirmTokenService;
    private readonly LinkBuilder _linkBuilder;
    private readonly IEventBus _eventBus;

    public CreateCompanyUserHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreateCompanyUserHandler> logger,
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
        CreateCompanyUserCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Validar que la empresa existe
            var company = await _dbContext.Companies.FirstOrDefaultAsync(
                c => c.Id == request.CompanyUser.CompanyId,
                cancellationToken
            );

            if (company is null)
                return new ApiResponse<bool>(false, "Company not found", false);

            // 2. Validar límite de usuarios
            var currentUserCount = await _dbContext.CompanyUsers.CountAsync(
                cu => cu.CompanyId == request.CompanyUser.CompanyId && cu.IsActive,
                cancellationToken
            );

            if (currentUserCount >= company.UserLimit)
                return new ApiResponse<bool>(
                    false,
                    $"User limit reached. Maximum allowed: {company.UserLimit}",
                    false
                );

            // 3. Validar que el email no exista
            var emailExists = await _dbContext.CompanyUsers.AnyAsync(
                cu => cu.Email == request.CompanyUser.Email,
                cancellationToken
            );

            if (emailExists)
                return new ApiResponse<bool>(false, "Email already exists", false);

            // 4. Crear el usuario
            request.CompanyUser.Id = Guid.NewGuid();
            request.CompanyUser.Password = _passwordHash.HashPassword(request.CompanyUser.Password);

            var companyUser = _mapper.Map<CompanyUser>(request.CompanyUser);
            companyUser.Confirm = false;
            companyUser.IsActive = false;
            companyUser.CreatedAt = DateTime.UtcNow;

            // 5. Asignar rol "User" por defecto
            var userRole = await _dbContext
                .Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

            if (userRole is null)
                return new(false, "User role not found", false);

            // 6. Crear el perfil asociado
            companyUser.CompanyUserProfile = new CompanyUserProfile
            {
                Id = Guid.NewGuid(),
                CompanyUserId = companyUser.Id,
                Name = request.CompanyUser.Name,
                LastName = request.CompanyUser.LastName,
                PhoneNumber = request.CompanyUser.Phone,
                Address = request.CompanyUser.Address,
                PhotoUrl = request.CompanyUser.PhotoUrl,
                Position = request.CompanyUser.Position,
                CreatedAt = DateTime.UtcNow,
            };

            // 7. Asignar rol
            await _dbContext.CompanyUserRoles.AddAsync(
                new CompanyUserRole
                {
                    Id = Guid.NewGuid(),
                    CompanyUserId = companyUser.Id,
                    RoleId = userRole.Id,
                    CreatedAt = DateTime.UtcNow,
                },
                cancellationToken
            );

            // 8. Generar token de confirmación
            var (token, expiration) = _confirmTokenService.Generate(
                companyUser.Id,
                companyUser.Email
            );
            companyUser.ConfirmToken = token;

            // 9. Guardar en BD
            await _dbContext.CompanyUsers.AddAsync(companyUser, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);

                string link = _linkBuilder.BuildConfirmationLink(
                    request.Origin,
                    companyUser.Email,
                    token
                );

                // 10. Publicar eventos
                _eventBus.Publish(
                    new AccountConfirmationLinkEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        companyUser.Id,
                        companyUser.Email,
                        $"{request.CompanyUser.Name} {request.CompanyUser.LastName}".Trim(),
                        link,
                        expiration,
                        false
                    )
                );

                _logger.LogInformation(
                    "Company user created successfully: {UserId}",
                    companyUser.Id
                );
                return new ApiResponse<bool>(true, "Company user created successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to create company user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating company user: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
