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
    private static readonly TimeSpan NotificationGracePeriod = TimeSpan.FromDays(3); // 3 dias sin notificar desde mismo lugar/device

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

            // 🔒 NUEVA LÓGICA: Revocar sesiones existentes de Customer
            await RevokeExistingCustomerSessionsAsync(data.CustomerId, ct);

            // 🌍 NUEVA LÓGICA: Verificar si debe enviar notificación para Customer
            var shouldSendNotification = await ShouldSendCustomerLoginNotificationAsync(
                data.CustomerId,
                req.IpAddress ?? "",
                req.Device ?? "",
                ct
            );

            // 3) Cargar roles y permisos reales del cliente
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

            // 4) generar token
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

            // 5) Crear sesión con geolocalización
            await CreateCustomerSessionAsync(data.CustomerId, sessionId, access, refresh, req, ct);

            // 6) Publicar evento SOLO si debe enviar notificación
            if (shouldSendNotification)
            {
                // Aquí podrías crear un CustomerLoginEvent si existe
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
                "Customer {CustomerId} ({Email}) logged in successfully. Session {SessionId} created. Notification sent: {NotificationSent}",
                data.CustomerId,
                req.Petition.Email,
                sessionId,
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
    /// 🔒 Revoca todas las sesiones activas del Customer (una sola sesión)
    /// </summary>
    private async Task RevokeExistingCustomerSessionsAsync(Guid customerId, CancellationToken ct)
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
    /// 🌍 Determina si debe enviar notificación de login para Customer
    /// </summary>
    private async Task<bool> ShouldSendCustomerLoginNotificationAsync(
        Guid customerId,
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
                _log.LogDebug(
                    "Skipping customer notification for {CustomerId} - local development environment (IP: {IpAddress})",
                    customerId,
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
    /// Crea sesión para Customer con geolocalización
    /// </summary>
    private async Task CreateCustomerSessionAsync(
        Guid customerId,
        Guid sessionId,
        TokenResult accessToken,
        TokenResult refreshToken,
        CustomerLoginCommand request,
        CancellationToken ct
    )
    {
        GeolocationInfo? geoInfo = null;
        string? displayLocation = null;

        try
        {
            geoInfo = await _geolocationService.GetLocationInfoAsync(request.IpAddress ?? "");
            displayLocation = await _geolocationService.GetLocationDisplayAsync(
                request.IpAddress ?? ""
            );

            _log.LogDebug(
                "Customer geolocation for IP {IpAddress}: {DisplayLocation} (Country: {Country}, City: {City})",
                request.IpAddress,
                displayLocation,
                geoInfo?.Country,
                geoInfo?.City
            );
        }
        catch (Exception ex)
        {
            _log.LogWarning(
                ex,
                "Failed to get customer geolocation for IP: {IpAddress}",
                request.IpAddress
            );
            displayLocation = "Unknown Location";
        }

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
            Latitude = geoInfo?.Latitude?.ToString("F6"),
            Longitude = geoInfo?.Longitude?.ToString("F6"),
            Location = displayLocation,
        };

        _db.CustomerSessions.Add(session);
        await _db.SaveChangesAsync(ct);
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

        // Normalizar user agent para detectar navegadores similares
        var deviceLower = device.ToLowerInvariant();

        // Detectar navegadores comunes
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

        // Para otros casos, usar hash para consistencia pero privacidad
        return $"device-{Math.Abs(device.GetHashCode() % 10000)}";
    }
}
