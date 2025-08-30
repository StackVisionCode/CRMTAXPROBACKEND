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

    // Configuración para notificaciones inteligentes
    private static readonly TimeSpan NotificationGracePeriod = TimeSpan.FromDays(3); // 3 días sin notificar desde mismo lugar/device

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

            // PASO 4: Verificar estado de la Company (SIN CustomPlans)
            if (!userResult.CompanyIsOperational)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Company is not operational (ServiceLevel: {ServiceLevel}, OwnerCount: {OwnerCount})",
                    request.Petition.Email,
                    userResult.CompanyServiceLevel,
                    userResult.CompanyOwnerCount
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "Company is not operational. Please contact your administrator."
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
                    "Login failed for {Email}: Not authorized for Staff portal. IsOwner: {IsOwner}, ServiceLevel: {ServiceLevel}, Roles: {Roles}",
                    request.Petition.Email,
                    userResult.IsOwner,
                    userResult.CompanyServiceLevel,
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

            // 🔒 PASO 7: Revocar sesiones existentes (una sola sesión activa)
            await RevokeExistingSessionsAsync(userResult.UserId, cancellationToken);

            // 🌍 PASO 8: Verificar si debe enviar notificación
            var shouldSendNotification = await ShouldSendLoginNotificationAsync(
                userResult.UserId,
                request.IpAddress ?? "",
                request.Device ?? "",
                cancellationToken
            );

            // PASO 9: Crear información para el token
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

            // PASO 10: Generar tokens
            var sessionId = Guid.NewGuid();
            var sessionInfo = new SessionInfo(sessionId);

            var accessTokenLifetime = request.Petition.RememberMe
                ? TimeSpan.FromDays(7)
                : TimeSpan.FromDays(1);
            var refreshTokenLifetime = request.Petition.RememberMe
                ? TimeSpan.FromDays(14)
                : TimeSpan.FromDays(7);

            var accessToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, accessTokenLifetime)
            );
            var refreshToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, refreshTokenLifetime)
            );

            // PASO 11: Crear sesión
            await CreateTaxUserSessionAsync(
                userResult.UserId,
                sessionId,
                accessToken,
                refreshToken,
                request,
                cancellationToken
            );

            // PASO 12: Publicar evento de login SOLO si debe enviar notificación
            if (shouldSendNotification)
            {
                var displayName = DetermineDisplayName(userResult);
                var loginEvent = new UserLoginEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    userResult.UserId,
                    userResult.Email,
                    userResult.Name,
                    userResult.LastName,
                    DateTime.UtcNow,
                    request.IpAddress ?? "",
                    request.Device,
                    userResult.CompanyId,
                    userResult.CompanyFullName,
                    userResult.CompanyName,
                    userResult.IsCompany,
                    userResult.CompanyDomain,
                    DateTime.Now.Year
                );

                _eventBus.Publish(loginEvent);
                _logger.LogInformation(
                    "Login notification sent for user {UserId} from new location/device",
                    userResult.UserId
                );
            }
            else
            {
                _logger.LogDebug(
                    "Login notification skipped for user {UserId} - recent login from same location/device",
                    userResult.UserId
                );
            }

            // PASO 13: Preparar respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = accessToken.AccessToken,
                ExpireTokenRequest = accessToken.ExpireAt,
                TokenRefresh = refreshToken.AccessToken,
            };

            _logger.LogInformation(
                "TaxUser {UserId} (IsOwner: {IsOwner}, ServiceLevel: {ServiceLevel}) logged in successfully. "
                    + "Session {SessionId} created. Notification sent: {NotificationSent}",
                userResult.UserId,
                userResult.IsOwner,
                userResult.CompanyServiceLevel,
                sessionId,
                shouldSendNotification
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
    /// Revoca todas las sesiones activas del usuario (una sola sesión)
    /// </summary>
    private async Task RevokeExistingSessionsAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var existingSessions = await _context
                .Sessions.Where(s =>
                    s.TaxUserId == userId && !s.IsRevoke && s.ExpireTokenRequest > DateTime.UtcNow
                )
                .ToListAsync(ct);

            if (existingSessions.Any())
            {
                foreach (var session in existingSessions)
                {
                    session.IsRevoke = true;
                    session.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Revoked {Count} existing sessions for user {UserId} to enforce single session policy",
                    existingSessions.Count,
                    userId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking existing sessions for user {UserId}", userId);
            // No lanzamos excepción para no interrumpir el login
        }
    }

    /// <summary>
    /// Determina si debe enviar notificación de login
    /// </summary>
    private async Task<bool> ShouldSendLoginNotificationAsync(
        Guid userId,
        string ipAddress,
        string device,
        CancellationToken ct
    )
    {
        try
        {
            // En desarrollo, las IPs locales no generan notificaciones
            if (IsLocalEnvironment(ipAddress))
            {
                _logger.LogDebug(
                    "Skipping notification for user {UserId} - local development environment (IP: {IpAddress})",
                    userId,
                    ipAddress
                );
                return false;
            }

            // Obtener información de geolocalización
            var geoInfo = await _geolocationService.GetLocationInfoAsync(ipAddress);
            var locationKey = GenerateLocationKey(geoInfo);
            var deviceKey = GenerateDeviceKey(device);

            // Buscar login reciente desde la misma ubicación/dispositivo
            var cutoffTime = DateTime.UtcNow.Subtract(NotificationGracePeriod);

            var recentSimilarLogin = await _context
                .Sessions.Where(s =>
                    s.TaxUserId == userId
                    && s.CreatedAt > cutoffTime
                    && (
                        s.Location == locationKey
                        || s.Device == deviceKey
                        || s.IpAddress == ipAddress
                    )
                )
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (recentSimilarLogin != null)
            {
                _logger.LogDebug(
                    "Recent login found for user {UserId} from similar location/device at {LoginTime}. Grace period active until {GracePeriodEnd}",
                    userId,
                    recentSimilarLogin.CreatedAt,
                    recentSimilarLogin.CreatedAt.Add(NotificationGracePeriod)
                );
                return false;
            }

            _logger.LogDebug(
                "No recent similar login found for user {UserId}. Notification will be sent. Location: {Location}, Device: {Device}",
                userId,
                locationKey,
                deviceKey
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking notification requirements for user {UserId}",
                userId
            );
            // En caso de error, por seguridad enviamos la notificación
            return true;
        }
    }

    /// <summary>
    /// Verifica si estamos en entorno de desarrollo
    /// </summary>
    private static bool IsLocalEnvironment(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return true;

        var localIndicators = new[] { "::1", "127.0.0.1", "localhost", "0.0.0.0", "unknown" };

        if (localIndicators.Contains(ipAddress, StringComparer.OrdinalIgnoreCase))
            return true;

        return ipAddress.StartsWith("192.168.")
            || ipAddress.StartsWith("10.")
            || ipAddress.StartsWith("172.16.")
            || ipAddress.StartsWith("172.17.")
            || ipAddress.StartsWith("172.18.")
            || ipAddress.StartsWith("172.19.")
            || ipAddress.StartsWith("172.20.")
            || ipAddress.StartsWith("172.21.")
            || ipAddress.StartsWith("172.22.")
            || ipAddress.StartsWith("172.23.")
            || ipAddress.StartsWith("172.24.")
            || ipAddress.StartsWith("172.25.")
            || ipAddress.StartsWith("172.26.")
            || ipAddress.StartsWith("172.27.")
            || ipAddress.StartsWith("172.28.")
            || ipAddress.StartsWith("172.29.")
            || ipAddress.StartsWith("172.30.")
            || ipAddress.StartsWith("172.31.");
    }

    /// <summary>
    /// Genera una clave de ubicación normalizada
    /// </summary>
    private static string GenerateLocationKey(GeolocationInfo? geoInfo)
    {
        if (geoInfo == null || geoInfo.Country == "Local")
            return "local-dev";

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(geoInfo.Country))
            parts.Add(geoInfo.Country.ToLowerInvariant());
        if (!string.IsNullOrEmpty(geoInfo.City))
            parts.Add(geoInfo.City.ToLowerInvariant());

        return parts.Any() ? string.Join("-", parts) : "unknown";
    }

    /// <summary>
    /// Genera una clave de dispositivo normalizada
    /// </summary>
    private static string GenerateDeviceKey(string? device)
    {
        if (string.IsNullOrWhiteSpace(device))
            return "unknown-device";

        var deviceLower = device.ToLowerInvariant();

        if (deviceLower.Contains("chrome"))
            return "chrome-browser";
        if (deviceLower.Contains("firefox"))
            return "firefox-browser";
        if (deviceLower.Contains("safari") && !deviceLower.Contains("chrome"))
            return "safari-browser";
        if (deviceLower.Contains("edge"))
            return "edge-browser";
        if (deviceLower.Contains("postman"))
            return "postman-client";
        if (deviceLower.Contains("insomnia"))
            return "insomnia-client";

        return $"device-{Math.Abs(device.GetHashCode() % 10000)}";
    }

    /// <summary>
    /// Busca TaxUser por email sin CustomPlans
    /// </summary>
    private async Task<TaxUserLoginResult?> FindTaxUserByEmailAsync(
        string email,
        CancellationToken ct
    )
    {
        var query =
            from u in _context.TaxUsers
            join c in _context.Companies on u.CompanyId equals c.Id
            where u.Email == email
            select new
            {
                // Datos del usuario
                UserId = u.Id,
                Email = u.Email,
                HashedPassword = u.Password,
                Name = u.Name,
                LastName = u.LastName,
                IsActive = u.IsActive,
                IsConfirmed = u.Confirm ?? false,
                IsOwner = u.IsOwner,

                // Datos de la company
                CompanyId = c.Id,
                CompanyName = c.CompanyName,
                CompanyFullName = c.FullName,
                CompanyDomain = c.Domain,
                IsCompany = c.IsCompany,
                CompanyServiceLevel = c.ServiceLevel,

                // Calcular si la company está operacional
                CompanyOwnerCount = _context.TaxUsers.Count(owner =>
                    owner.CompanyId == c.Id && owner.IsOwner && owner.IsActive
                ),
            };

        var result = await query.FirstOrDefaultAsync(ct);

        if (result == null)
            return null;

        return new TaxUserLoginResult
        {
            UserId = result.UserId,
            Email = result.Email,
            HashedPassword = result.HashedPassword,
            Name = result.Name,
            LastName = result.LastName,
            IsActive = result.IsActive,
            IsConfirmed = result.IsConfirmed,
            IsOwner = result.IsOwner,
            CompanyId = result.CompanyId,
            CompanyName = result.CompanyName,
            CompanyFullName = result.CompanyFullName,
            CompanyDomain = result.CompanyDomain,
            IsCompany = result.IsCompany,
            CompanyServiceLevel = result.CompanyServiceLevel,
            CompanyOwnerCount = result.CompanyOwnerCount,
            CompanyIsOperational = result.CompanyOwnerCount > 0, // Company operacional si tiene owners activos
        };
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
            where ur.TaxUserId == userId && p.IsGranted
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
        // Obtener geolocalización
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
            return user.CompanyName;

        if (!user.IsCompany && !string.IsNullOrWhiteSpace(user.CompanyFullName))
            return user.CompanyFullName;

        if (!string.IsNullOrWhiteSpace(user.Name) || !string.IsNullOrWhiteSpace(user.LastName))
            return $"{user.Name} {user.LastName}".Trim();

        return user.Email;
    }
}
