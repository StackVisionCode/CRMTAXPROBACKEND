using System.Text.RegularExpressions;
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
            // 🔍 PASO 1: VALIDACIONES DE ENTRADA
            var inputValidation = ValidateInputData(request.CompanyUser);
            if (inputValidation.Success == null || !inputValidation.Success.Value)
                return inputValidation;

            // 🏢 PASO 2: VALIDAR EMPRESA SIN Include - JOIN EXPLÍCITO
            var companyData = await (
                from c in _dbContext.Companies
                where c.Id == request.CompanyUser.CompanyId
                select new
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    UserLimit = c.UserLimit,
                    IsActive = true,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (companyData is null)
            {
                _logger.LogWarning(
                    "Attempt to create user for non-existent company: {CompanyId}",
                    request.CompanyUser.CompanyId
                );
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            // 📧 PASO 3: VALIDACIÓN CRÍTICA - EMAIL NO PUEDE PERTENECER A OTRA EMPRESA
            var emailConflictData = await (
                from cu in _dbContext.CompanyUsers
                where cu.Email == request.CompanyUser.Email
                select new
                {
                    ExistingCompanyId = cu.CompanyId,
                    ExistingUserId = cu.Id,
                    IsActive = cu.IsActive,
                    IsConfirmed = cu.Confirm,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (emailConflictData != null)
            {
                // 🚨 CONFLICTO CRÍTICO: Email ya existe en OTRA empresa
                if (emailConflictData.ExistingCompanyId != request.CompanyUser.CompanyId)
                {
                    _logger.LogWarning(
                        "Email {Email} already belongs to company {ExistingCompanyId}, cannot create in company {NewCompanyId}",
                        request.CompanyUser.Email,
                        emailConflictData.ExistingCompanyId,
                        request.CompanyUser.CompanyId
                    );
                    return new ApiResponse<bool>(
                        false,
                        "This email address is already registered with another company",
                        false
                    );
                }

                // 🚨 CONFLICTO EN LA MISMA EMPRESA: Email ya existe en esta empresa
                _logger.LogWarning(
                    "Email {Email} already exists in company {CompanyId}. User: {UserId}",
                    request.CompanyUser.Email,
                    request.CompanyUser.CompanyId,
                    emailConflictData.ExistingUserId
                );
                return new ApiResponse<bool>(
                    false,
                    "This email address is already registered in your company",
                    false
                );
            }

            // 📧 PASO 4: VALIDACIÓN CRUZADA - VERIFICAR EN TAXUSERS (STAFF)
            var emailInTaxUsers = await _dbContext
                .TaxUsers.Where(tu => tu.Email == request.CompanyUser.Email)
                .Select(tu => new { tu.Id, tu.Email })
                .AnyAsync(cancellationToken);

            if (emailInTaxUsers)
            {
                _logger.LogWarning(
                    "Email {Email} already exists as staff user, cannot create as company user",
                    request.CompanyUser.Email
                );
                return new ApiResponse<bool>(
                    false,
                    "This email address is already registered as a staff member",
                    false
                );
            }

            // 👥 PASO 5: VALIDAR LÍMITE DE USUARIOS ACTIVOS SIN Include
            var currentActiveUsers = await _dbContext
                .CompanyUsers.Where(cu =>
                    cu.CompanyId == request.CompanyUser.CompanyId && cu.IsActive
                )
                .CountAsync(cancellationToken);

            if (currentActiveUsers >= companyData.UserLimit)
            {
                _logger.LogWarning(
                    "Company {CompanyId} has reached user limit. Current: {Current}, Limit: {Limit}",
                    request.CompanyUser.CompanyId,
                    currentActiveUsers,
                    companyData.UserLimit
                );
                return new ApiResponse<bool>(
                    false,
                    $"User limit reached. Maximum allowed: {companyData.UserLimit}, current: {currentActiveUsers}",
                    false
                );
            }

            // 🔑 PASO 6: VALIDAR ROL "USER" EXISTE SIN Include
            var userRoleData = await _dbContext
                .Roles.Where(r => r.Name == "User" && r.PortalAccess == PortalAccess.Staff)
                .Select(r => new { r.Id, r.Name })
                .FirstOrDefaultAsync(cancellationToken);

            if (userRoleData is null)
            {
                _logger.LogError("Default 'User' role not found in system");
                return new ApiResponse<bool>(
                    false,
                    "System configuration error: Default user role not found",
                    false
                );
            }

            // 🛡️ PASO 7: CREAR USUARIO CON VALORES SEGUROS
            var userId = Guid.NewGuid();
            var profileId = Guid.NewGuid();
            var userRoleId = Guid.NewGuid();

            var hashedPassword = _passwordHash.HashPassword(request.CompanyUser.Password);
            var (confirmToken, confirmExpiration) = _confirmTokenService.Generate(
                userId,
                request.CompanyUser.Email
            );

            // 📝 PASO 8: CREAR COMPANYUSER (SIN NAVEGACIÓN)
            var companyUser = new CompanyUser
            {
                Id = userId,
                CompanyId = request.CompanyUser.CompanyId,
                Email = request.CompanyUser.Email.ToLowerInvariant().Trim(),
                Password = hashedPassword,
                IsActive = false,
                Confirm = false,
                ConfirmToken = confirmToken,
                Factor2 = false,
                OtpVerified = false,
                CreatedAt = DateTime.UtcNow,
            };

            // 📝 PASO 9: CREAR COMPANYUSERPROFILE (SIN NAVEGACIÓN)
            var companyUserProfile = new CompanyUserProfile
            {
                Id = profileId,
                CompanyUserId = userId,
                Name = request.CompanyUser.Name?.Trim(),
                LastName = request.CompanyUser.LastName?.Trim(),
                PhoneNumber = request.CompanyUser.Phone?.Trim(),
                Address = request.CompanyUser.Address?.Trim(),
                PhotoUrl = request.CompanyUser.PhotoUrl?.Trim(),
                Position = request.CompanyUser.Position?.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            // 🔑 PASO 10: CREAR COMPANYUSERROLE (SIN NAVEGACIÓN)
            var companyUserRole = new CompanyUserRole
            {
                Id = userRoleId,
                CompanyUserId = userId,
                RoleId = userRoleData.Id,
                CreatedAt = DateTime.UtcNow,
            };

            // 💾 PASO 11: GUARDAR TODO EN BD
            _dbContext.CompanyUsers.Add(companyUser);
            _dbContext.CompanyUserProfiles.Add(companyUserProfile);
            _dbContext.CompanyUserRoles.Add(companyUserRole);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);

                // 🔗 PASO 12: GENERAR LINK DE CONFIRMACIÓN
                string confirmationLink = _linkBuilder.BuildConfirmationLink(
                    request.Origin,
                    companyUser.Email,
                    confirmToken
                );

                // 📧 PASO 13: PUBLICAR EVENTO DE CONFIRMACIÓN
                _eventBus.Publish(
                    new AccountConfirmationLinkEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        userId,
                        companyUser.Email,
                        $"{request.CompanyUser.Name} {request.CompanyUser.LastName}".Trim(),
                        confirmationLink,
                        confirmExpiration,
                        false
                    )
                );

                _logger.LogInformation(
                    "Company user created successfully: UserId={UserId}, Email={Email}, Company={CompanyId}, Profile={ProfileId}",
                    userId,
                    companyUser.Email,
                    request.CompanyUser.CompanyId,
                    profileId
                );

                return new ApiResponse<bool>(
                    true,
                    "Company user created successfully. Confirmation email sent.",
                    true
                );
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to save company user to database");
                return new ApiResponse<bool>(false, "Failed to create company user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error creating company user. Email={Email}, CompanyId={CompanyId}, Error={Message}",
                request.CompanyUser.Email,
                request.CompanyUser.CompanyId,
                ex.Message
            );
            return new ApiResponse<bool>(false, "An error occurred while creating the user", false);
        }
    }

    /// <summary>
    /// Valida los datos de entrada del usuario
    /// </summary>
    private static ApiResponse<bool> ValidateInputData(dynamic companyUser)
    {
        // Validar email format
        if (string.IsNullOrWhiteSpace(companyUser.Email))
            return new ApiResponse<bool>(false, "Email is required", false);

        var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase);
        if (!emailRegex.IsMatch(companyUser.Email))
            return new ApiResponse<bool>(false, "Invalid email format", false);

        // Validar longitud del email
        if (companyUser.Email.Length > 320)
            return new ApiResponse<bool>(false, "Email address is too long", false);

        // Validar password strength
        if (string.IsNullOrWhiteSpace(companyUser.Password))
            return new ApiResponse<bool>(false, "Password is required", false);

        if (companyUser.Password.Length < 8)
            return new ApiResponse<bool>(
                false,
                "Password must be at least 8 characters long",
                false
            );

        // Validar nombre y apellido
        if (string.IsNullOrWhiteSpace(companyUser.Name))
            return new ApiResponse<bool>(false, "Name is required", false);

        if (string.IsNullOrWhiteSpace(companyUser.LastName))
            return new ApiResponse<bool>(false, "Last name is required", false);

        // Validar CompanyId
        if (companyUser.CompanyId == Guid.Empty)
            return new ApiResponse<bool>(false, "Valid Company ID is required", false);

        return new ApiResponse<bool>(true, "Validation passed", true);
    }
}
