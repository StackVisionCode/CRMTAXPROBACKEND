using AuthService.Applications.Services;
using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using AuthService.Infraestructure.Services;
using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace Handlers.SessionHandlers;

public class LoginHandler : IRequestHandler<LoginCommands, ApiResponse<LoginResponseDTO>>
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHash _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IEventBus _eventBus;
    private readonly IGeolocationService _geolocationService;

    public LoginHandler(
        ApplicationDbContext context,
        IPasswordHash passwordHasher,
        ITokenService tokenService,
        ILogger<LoginHandler> logger,
        IEventBus eventBus,
        IGeolocationService geolocationService
    )
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
        _eventBus = eventBus;
        _geolocationService = geolocationService;
    }

    public async Task<ApiResponse<LoginResponseDTO>> Handle(
        LoginCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // PASO 1: Buscar TaxUser por email
            var userResult = await FindTaxUserByEmailAsync(
                request.Petition.Email,
                cancellationToken
            );

            if (userResult == null)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: User not found",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            // PASO 2: Verificar contraseña
            if (!_passwordHasher.Verify(request.Petition.Password, userResult.HashedPassword))
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Invalid password",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            // PASO 3: Verificar estado del usuario
            if (!userResult.IsActive)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: User is inactive",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "User account is inactive");
            }

            if (!userResult.IsConfirmed)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Account not confirmed",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "Please confirm your account first"
                );
            }

            // PASO 4: Verificar estado de la Company
            if (!userResult.CompanyIsActive)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Company plan is inactive",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "Company subscription is inactive. Please contact your administrator."
                );
            }

            // PASO 5: Obtener roles y permisos del TaxUser
            var rolesAndPermissions = await GetTaxUserRolesAndPermissionsAsync(
                userResult.UserId,
                cancellationToken
            );

            // 🚪 PASO 6: Verificar autorización para Staff portal
            var allowedPortals = rolesAndPermissions
                .Roles.Select(r => r.PortalAccess)
                .Distinct()
                .ToList();

            bool allowed = allowedPortals.Any(p =>
                p == PortalAccess.Staff || p == PortalAccess.Developer || p == PortalAccess.Both
            );

            if (!allowed)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Not authorized for Staff portal. IsOwner: {IsOwner}, Roles: {Roles}",
                    request.Petition.Email,
                    userResult.IsOwner,
                    string.Join(",", rolesAndPermissions.Roles.Select(r => r.Name))
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "You do not have permission to log in here."
                )
                {
                    StatusCode = 403,
                };
            }

            // PASO 7: Crear información para el token
            var userInfo = new UserInfo(
                UserId: userResult.UserId,
                Email: userResult.Email,
                Name: userResult.Name,
                LastName: userResult.LastName,
                CompanyId: userResult.CompanyId,
                CompanyName: userResult.CompanyName,
                CompanyDomain: userResult.CompanyDomain,
                IsCompany: userResult.IsCompany,
                IsOwner: userResult.IsOwner,
                Roles: rolesAndPermissions.Roles.Select(r => r.Name),
                Permissions: rolesAndPermissions.Permissions.Select(p => p.Code),
                Portals: allowedPortals.Select(p => p.ToString()).ToList()
            );

            // PASO 8: Generar tokens
            var sessionId = Guid.NewGuid();
            var sessionInfo = new SessionInfo(sessionId);

            var accessTokenLifetime = request.Petition.RememberMe
                ? TimeSpan.FromDays(30)
                : TimeSpan.FromDays(1);
            var refreshTokenLifetime = request.Petition.RememberMe
                ? TimeSpan.FromDays(90)
                : TimeSpan.FromDays(7);

            var accessToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, accessTokenLifetime)
            );
            var refreshToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, refreshTokenLifetime)
            );

            //  PASO 9: Crear sesión
            await CreateTaxUserSessionAsync(
                userResult.UserId,
                sessionId,
                accessToken,
                refreshToken,
                request,
                cancellationToken
            );

            // PASO 10: Publicar evento de login
            var displayName = DetermineDisplayName(userResult);
            var loginEvent = new UserLoginEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                userResult.UserId,
                userResult.Email,
                userResult.Name,
                userResult.LastName,
                DateTime.UtcNow,
                request.IpAddress,
                request.Device,
                userResult.CompanyId,
                userResult.CompanyFullName,
                userResult.CompanyName,
                userResult.IsCompany,
                userResult.CompanyDomain,
                DateTime.Now.Year
            );

            _eventBus.Publish(loginEvent);

            // PASO 11: Preparar respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = accessToken.AccessToken,
                ExpireTokenRequest = accessToken.ExpireAt,
                TokenRefresh = refreshToken.AccessToken,
            };

            _logger.LogInformation(
                "TaxUser {UserId} (IsOwner: {IsOwner}) logged in successfully. Session {SessionId} created",
                userResult.UserId,
                userResult.IsOwner,
                sessionId
            );

            return new ApiResponse<LoginResponseDTO>(true, "Login successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for {Email}", request.Petition.Email);
            return new ApiResponse<LoginResponseDTO>(false, "An error occurred during login");
        }
    }

    /// <summary>
    /// Busca TaxUser por email con información de Company
    /// </summary>
    private async Task<TaxUserLoginResult?> FindTaxUserByEmailAsync(
        string email,
        CancellationToken ct
    )
    {
        var query =
            from u in _context.TaxUsers
            join c in _context.Companies on u.CompanyId equals c.Id
            join cp in _context.CustomPlans on c.CustomPlanId equals cp.Id
            where u.Email == email
            select new TaxUserLoginResult
            {
                UserId = u.Id,
                Email = u.Email,
                HashedPassword = u.Password,
                Name = u.Name,
                LastName = u.LastName,
                IsActive = u.IsActive,
                IsConfirmed = u.Confirm ?? false,
                IsOwner = u.IsOwner,
                CompanyId = c.Id,
                CompanyName = c.CompanyName,
                CompanyFullName = c.FullName,
                CompanyDomain = c.Domain,
                IsCompany = c.IsCompany,
                CompanyIsActive = cp.IsActive, // Verificar si el plan está activo
            };

        return await query.FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Obtiene roles y permisos del TaxUser, incluyendo permisos personalizados
    /// </summary>
    private async Task<(
        List<RoleResult> Roles,
        List<PermissionResult> Permissions
    )> GetTaxUserRolesAndPermissionsAsync(Guid userId, CancellationToken ct)
    {
        // Obtener roles del TaxUser
        var roles = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where ur.TaxUserId == userId
            select new RoleResult
            {
                Id = r.Id,
                Name = r.Name,
                PortalAccess = r.PortalAccess,
            }
        ).ToListAsync(ct);

        // Obtener permisos base de los roles
        var rolePermissions = await (
            from ur in _context.UserRoles
            join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _context.Permissions on rp.PermissionId equals p.Id
            where ur.TaxUserId == userId && p.IsGranted // Solo permisos activos globalmente
            select new PermissionResult
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
            }
        ).ToListAsync(ct);

        // Obtener permisos personalizados granted
        var customPermissionsGranted = await (
            from cp in _context.CompanyPermissions
            join p in _context.Permissions on cp.PermissionId equals p.Id
            where cp.TaxUserId == userId && cp.IsGranted && p.IsGranted
            select new PermissionResult
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
            }
        ).ToListAsync(ct);

        // Obtener códigos de permisos revocados
        var revokedPermissionCodes = await (
            from cp in _context.CompanyPermissions
            join p in _context.Permissions on cp.PermissionId equals p.Id
            where cp.TaxUserId == userId && !cp.IsGranted
            select p.Code
        ).ToListAsync(ct);

        // Combinar permisos: (roles + custom granted) - revoked
        var allPermissions = rolePermissions
            .Concat(customPermissionsGranted)
            .Where(p => !revokedPermissionCodes.Contains(p.Code))
            .DistinctBy(p => p.Code)
            .ToList();

        return (roles, allPermissions);
    }

    /// <summary>
    /// Crea sesión para TaxUser
    /// </summary>
    private async Task CreateTaxUserSessionAsync(
        Guid userId,
        Guid sessionId,
        TokenResult accessToken,
        TokenResult refreshToken,
        LoginCommands request,
        CancellationToken ct
    )
    {
        // 🆕 OBTENER GEOLOCALIZACIÓN USANDO TU SERVICIO EXISTENTE
        GeolocationInfo? geoInfo = null;
        string? displayLocation = null;

        try
        {
            geoInfo = await _geolocationService.GetLocationInfoAsync(request.IpAddress ?? "");
            displayLocation = await _geolocationService.GetLocationDisplayAsync(
                request.IpAddress ?? ""
            );

            _logger.LogDebug(
                "Geolocation for IP {IpAddress}: {DisplayLocation} (Country: {Country}, City: {City})",
                request.IpAddress,
                displayLocation,
                geoInfo?.Country,
                geoInfo?.City
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to get geolocation for IP: {IpAddress}",
                request.IpAddress
            );
            displayLocation = "Unknown Location";
        }

        var session = new Session
        {
            Id = sessionId,
            TaxUserId = userId,
            TokenRequest = accessToken.AccessToken,
            ExpireTokenRequest = accessToken.ExpireAt,
            TokenRefresh = refreshToken.AccessToken,
            IpAddress = request.IpAddress,
            Device = request.Device,
            IsRevoke = false,
            CreatedAt = DateTime.UtcNow,
            Country = geoInfo?.Country,
            City = geoInfo?.City,
            Region = geoInfo?.Region,
            Latitude = geoInfo?.Latitude?.ToString("F6"),
            Longitude = geoInfo?.Longitude?.ToString("F6"),
            Location = displayLocation,
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Determina el nombre para mostrar
    /// </summary>
    private static string DetermineDisplayName(TaxUserLoginResult user)
    {
        if (user.IsCompany && !string.IsNullOrWhiteSpace(user.CompanyName))
        {
            return user.CompanyName;
        }

        if (!user.IsCompany && !string.IsNullOrWhiteSpace(user.CompanyFullName))
        {
            return user.CompanyFullName;
        }

        if (!string.IsNullOrWhiteSpace(user.Name) || !string.IsNullOrWhiteSpace(user.LastName))
        {
            return $"{user.Name} {user.LastName}".Trim();
        }

        return user.Email;
    }
}
