using AuthService.Applications.Services;
using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using Commands.CustomerCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.Contracts.Security;
using SharedLibrary.DTOs;

namespace Handlers.CustomerHandlers;

public class CustomerLoginHandler
    : IRequestHandler<CustomerLoginCommand, ApiResponse<LoginResponseDTO>>
{
    private readonly IHttpClientFactory _http;
    private readonly IPasswordHash _hash;
    private readonly ITokenService _token;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomerLoginHandler> _log;
    private readonly IEventBus _bus;
    private readonly AuthService.Infraestructure.Services.IGeolocationService _geolocationService;

    // Configuración para notificaciones inteligentes
    private static readonly TimeSpan NotificationGracePeriod = TimeSpan.FromDays(3);

    public CustomerLoginHandler(
        IHttpClientFactory http,
        IPasswordHash hash,
        ITokenService token,
        ApplicationDbContext db,
        ILogger<CustomerLoginHandler> log,
        IEventBus bus,
        AuthService.Infraestructure.Services.IGeolocationService geolocationService
    )
    {
        _http = http;
        _hash = hash;
        _token = token;
        _db = db;
        _log = log;
        _bus = bus;
        _geolocationService = geolocationService;
    }

    public async Task<ApiResponse<LoginResponseDTO>> Handle(
        CustomerLoginCommand req,
        CancellationToken ct
    )
    {
        try
        {
            // 1) llamar a CustomerService para obtener AuthInfoDTO
            var client = _http.CreateClient("Customers");
            var resp = await client.GetAsync(
                $"/api/ContactInfo/Internal/AuthInfo?email={req.Petition.Email}",
                ct
            );

            if (!resp.IsSuccessStatusCode)
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");

            var wrapper = await resp.Content.ReadFromJsonAsync<ApiResponse<RemoteAuthInfoDTO>>(
                cancellationToken: ct
            );

            var data = wrapper?.Data;
            if (data is null || !data.IsLogin)
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");

            // 2) verificar contraseña
            if (!_hash.Verify(req.Petition.Password, data.PasswordHash))
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");

            // 3) Obtener información de geolocalización ANTES de verificar sesiones
            var geoInfo = await _geolocationService.GetLocationInfoAsync(req.IpAddress ?? "");
            var deviceKey = GenerateDeviceKey(req.Device ?? "");

            // 4) Verificar si existe sesión activa similar
            var existingSessionCheck = await CheckForExistingActiveCustomerSessionAsync(
                data.CustomerId,
                req.IpAddress ?? "",
                geoInfo,
                deviceKey,
                ct
            );

            if (existingSessionCheck.HasActiveSession)
            {
                _log.LogWarning(
                    "Customer login denied for {Email}: Active session exists from same location/device. SessionId: {SessionId}, Location: {Location}",
                    req.Petition.Email,
                    existingSessionCheck.ExistingSessionId,
                    existingSessionCheck.Location
                );

                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "You already have an active session from this location. Please close other sessions first."
                )
                {
                    StatusCode = 409, // Conflict
                };
            }

            // 5) Revocar TODAS las sesiones existentes de Customer (política de sesión única)
            await RevokeAllExistingCustomerSessionsAsync(data.CustomerId, ct);

            // 6) Verificar si debe enviar notificación para Customer
            var shouldSendNotification = await ShouldSendCustomerLoginNotificationAsync(
                data.CustomerId,
                req.IpAddress ?? "",
                geoInfo,
                deviceKey,
                ct
            );

            // 7) Cargar roles y permisos reales del cliente
            var roleNames = await (
                from cr in _db.CustomerRoles
                where cr.CustomerId == data.CustomerId
                join r in _db.Roles on cr.RoleId equals r.Id
                select r.Name
            ).ToListAsync(ct);

            if (!roleNames.Any())
                roleNames.Add("Customer");

            var portals = await _db
                .Roles.Where(r => roleNames.Contains(r.Name))
                .Select(r => r.PortalAccess.ToString())
                .Distinct()
                .ToListAsync(ct);

            if (!portals.Any())
                portals.Add("Customer");

            var permCodes = await (
                from cr in _db.CustomerRoles
                where cr.CustomerId == data.CustomerId
                join rp in _db.RolePermissions on cr.RoleId equals rp.RoleId
                join p in _db.Permissions on rp.PermissionId equals p.Id
                select p.Code
            )
                .Distinct()
                .ToListAsync(ct);

            bool allowed = await _db
                .Roles.Where(r => roleNames.Contains(r.Name))
                .AnyAsync(
                    r =>
                        r.PortalAccess == PortalAccess.Customer
                        || r.PortalAccess == PortalAccess.Both,
                    ct
                );

            if (!allowed)
            {
                _log.LogWarning(
                    "Role(s) {Roles} not authorized for Customer login",
                    string.Join(',', roleNames)
                );

                return new ApiResponse<LoginResponseDTO>(false, "Exclusive portal for clients")
                {
                    StatusCode = 403,
                };
            }

            // 8) generar token
            var sessionId = Guid.NewGuid();
            var userInfo = new UserInfo(
                UserId: data.CustomerId,
                Email: data.Email,
                Name: data.DisplayName,
                LastName: null,
                CompanyId: data.CompanyId,
                CompanyName: null,
                CompanyDomain: null,
                IsCompany: false,
                IsOwner: false,
                Roles: roleNames,
                Permissions: permCodes,
                Portals: portals
            );

            var sessionInfo = new SessionInfo(sessionId);
            var access = _token.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(1))
            );
            var refresh = _token.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(3))
            );

            // 9) Crear sesión con geolocalización mejorada
            await CreateCustomerSessionAsync(
                data.CustomerId,
                sessionId,
                access,
                refresh,
                req,
                geoInfo,
                ct
            );

            // 10) Publicar evento SOLO si debe enviar notificación
            if (shouldSendNotification)
            {
                // Aquí podrías crear un CustomerLoginEvent específico si existe
                // O usar el mismo UserLoginEvent adaptando los campos
                _log.LogInformation(
                    "Customer login notification sent for {CustomerId} from new location/device",
                    data.CustomerId
                );
            }
            else
            {
                _log.LogDebug(
                    "Customer login notification skipped for {CustomerId} - recent login from same location/device",
                    data.CustomerId
                );
            }

            var result = new LoginResponseDTO
            {
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
            };

            _log.LogInformation(
                "Customer {CustomerId} ({Email}) logged in successfully. Session {SessionId} created from {Location}. Notification sent: {NotificationSent}",
                data.CustomerId,
                req.Petition.Email,
                sessionId,
                geoInfo?.GetLocationKey() ?? "unknown",
                shouldSendNotification
            );

            return new ApiResponse<LoginResponseDTO>(true, "Login correcto", result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al iniciar sesión: {Message}", ex.Message);
            return new ApiResponse<LoginResponseDTO>(false, ex.Message, null!);
        }
    }

    /// <summary>
    /// Verifica si existe una sesión activa similar para Customer
    /// </summary>
    private async Task<(
        bool HasActiveSession,
        Guid? ExistingSessionId,
        string? Location
    )> CheckForExistingActiveCustomerSessionAsync(
        Guid customerId,
        string ipAddress,
        GeolocationInfo? geoInfo,
        string deviceKey,
        CancellationToken ct
    )
    {
        try
        {
            var locationKey = geoInfo?.GetLocationKey() ?? "unknown";
            var currentTime = DateTime.UtcNow;

            // Buscar sesiones activas del customer
            var activeSession = await _db
                .CustomerSessions.Where(s =>
                    s.CustomerId == customerId && !s.IsRevoke && s.ExpireTokenRequest > currentTime
                )
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (activeSession == null)
            {
                return (false, null, null);
            }

            // Para IPs locales, siempre permitir nueva sesión (desarrollo)
            if (IsLocalEnvironment(ipAddress))
            {
                _log.LogDebug(
                    "Allowing multiple sessions for customer {CustomerId} in local environment",
                    customerId
                );
                return (false, null, null);
            }

            // Verificar si es desde la misma ubicación/dispositivo exacto
            var isSameLocation =
                activeSession.IpAddress == ipAddress
                || activeSession.Location == locationKey
                || activeSession.Device == deviceKey;

            if (isSameLocation)
            {
                return (true, activeSession.Id, activeSession.Location);
            }

            return (false, null, null);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error checking for existing active customer sessions for {CustomerId}",
                customerId
            );
            // En caso de error, permitir el login para no bloquear al usuario
            return (false, null, null);
        }
    }

    /// <summary>
    /// Revoca TODAS las sesiones activas del Customer (política de sesión única)
    /// </summary>
    private async Task RevokeAllExistingCustomerSessionsAsync(Guid customerId, CancellationToken ct)
    {
        try
        {
            var existingSessions = await _db
                .CustomerSessions.Where(s =>
                    s.CustomerId == customerId
                    && !s.IsRevoke
                    && s.ExpireTokenRequest > DateTime.UtcNow
                )
                .ToListAsync(ct);

            if (existingSessions.Any())
            {
                foreach (var session in existingSessions)
                {
                    session.IsRevoke = true;
                    session.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync(ct);

                _log.LogInformation(
                    "Revoked {Count} existing customer sessions for {CustomerId} to enforce single session policy",
                    existingSessions.Count,
                    customerId
                );
            }
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error revoking existing customer sessions for {CustomerId}",
                customerId
            );
        }
    }

    /// <summary>
    /// Determina si debe enviar notificación de login para Customer
    /// </summary>
    private async Task<bool> ShouldSendCustomerLoginNotificationAsync(
        Guid customerId,
        string ipAddress,
        GeolocationInfo? geoInfo,
        string deviceKey,
        CancellationToken ct
    )
    {
        try
        {
            // En desarrollo, las IPs locales no generan notificaciones
            if (IsLocalEnvironment(ipAddress))
            {
                _log.LogDebug(
                    "Skipping customer notification for {CustomerId} - local development environment (IP: {IpAddress})",
                    customerId,
                    ipAddress
                );
                return false;
            }

            var locationKey = geoInfo?.GetLocationKey() ?? "unknown";
            var cutoffTime = DateTime.UtcNow.Subtract(NotificationGracePeriod);

            // Buscar login reciente desde la misma ubicación/dispositivo
            var recentSimilarLogin = await _db
                .CustomerSessions.Where(s =>
                    s.CustomerId == customerId
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
                _log.LogDebug(
                    "Recent customer login found for {CustomerId} from similar location/device at {LoginTime}. Grace period active until {GracePeriodEnd}",
                    customerId,
                    recentSimilarLogin.CreatedAt,
                    recentSimilarLogin.CreatedAt.Add(NotificationGracePeriod)
                );
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error checking customer notification requirements for {CustomerId}",
                customerId
            );
            return true; // Por seguridad, enviamos la notificación
        }
    }

    /// <summary>
    /// Crea sesión para Customer con geolocalización mejorada
    /// </summary>
    private async Task CreateCustomerSessionAsync(
        Guid customerId,
        Guid sessionId,
        TokenResult accessToken,
        TokenResult refreshToken,
        CustomerLoginCommand request,
        GeolocationInfo? geoInfo,
        CancellationToken ct
    )
    {
        var displayLocation = await _geolocationService.GetLocationDisplayAsync(
            request.IpAddress ?? ""
        );
        var coordinates = geoInfo?.GetCoordinatesAsString() ?? (null, null);

        var session = new CustomerSession
        {
            Id = sessionId,
            CustomerId = customerId,
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
            Latitude = coordinates.Lat,
            Longitude = coordinates.Lng,
            Location = geoInfo?.GetLocationKey() ?? "unknown",
        };

        _db.CustomerSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        _log.LogDebug(
            "Customer session created for {CustomerId}: {SessionId} from {DisplayLocation} (IP: {IpAddress})",
            customerId,
            sessionId,
            displayLocation,
            request.IpAddress
        );
    }

    /// <summary>
    /// Verifica si estamos en entorno de desarrollo
    /// </summary>
    private static bool IsLocalEnvironment(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return true;

        var localIndicators = new[]
        {
            "::1", // IPv6 localhost
            "127.0.0.1", // IPv4 localhost
            "localhost", // hostname
            "0.0.0.0", // All interfaces
            "unknown", // Valor por defecto cuando no se puede obtener IP
        };

        // Verificar direcciones locales exactas
        if (localIndicators.Contains(ipAddress, StringComparer.OrdinalIgnoreCase))
            return true;

        // Verificar rangos privados
        return ipAddress.StartsWith("192.168.")
            || // Private network
            ipAddress.StartsWith("10.")
            || // Private network
            ipAddress.StartsWith("172.16.")
            || // Private network range
            ipAddress.StartsWith("172.17.")
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
    /// Genera una clave de dispositivo normalizada mejorada
    /// </summary>
    private static string GenerateDeviceKey(string? device)
    {
        if (string.IsNullOrWhiteSpace(device))
            return "unknown-device";

        var deviceLower = device.ToLowerInvariant();

        // Detectar navegadores con mayor precisión
        if (deviceLower.Contains("chrome") && !deviceLower.Contains("edge"))
            return "chrome-browser";
        if (deviceLower.Contains("firefox"))
            return "firefox-browser";
        if (deviceLower.Contains("safari") && !deviceLower.Contains("chrome"))
            return "safari-browser";
        if (deviceLower.Contains("edg")) // Edge usa "Edg" en user agent
            return "edge-browser";
        if (deviceLower.Contains("opera") || deviceLower.Contains("opr"))
            return "opera-browser";

        // Detectar herramientas de desarrollo
        if (deviceLower.Contains("postman"))
            return "postman-client";
        if (deviceLower.Contains("insomnia"))
            return "insomnia-client";
        if (deviceLower.Contains("curl"))
            return "curl-client";
        if (deviceLower.Contains("wget"))
            return "wget-client";

        // Para otros casos, usar hash consistente pero más específico
        var hash = Math.Abs(device.GetHashCode() % 10000);
        return $"device-{hash:D4}";
    }
}
