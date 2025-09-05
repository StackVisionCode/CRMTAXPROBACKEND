using System.Globalization;
using System.Net;
using AuthService.Infraestructure.Services;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using SharedLibrary.Caching;

namespace AuthService.Applications.Services;

public class GeolocationService : IGeolocationService, IDisposable
{
    private readonly ILogger<GeolocationService> _logger;
    private readonly IHybridCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DatabaseReader? _mmdbReader;

    public GeolocationService(
        ILogger<GeolocationService> logger,
        IHybridCache cache,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        // Cargar MaxMind como fallback opcional
        _mmdbReader = InitializeMaxMindDatabase();

        _logger.LogInformation(
            "GeolocationService initialized. Cloudflare-first approach. MaxMind fallback: {MaxMindAvailable}",
            _mmdbReader != null
        );
    }

    private DatabaseReader? InitializeMaxMindDatabase()
    {
        var mmdbPath = _configuration["GeoIP:MmdbPath"];
        if (string.IsNullOrWhiteSpace(mmdbPath) || !File.Exists(mmdbPath))
        {
            return null; // No es crítico
        }

        try
        {
            return new DatabaseReader(mmdbPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "MaxMind database could not be loaded, using Cloudflare-only approach"
            );
            return null;
        }
    }

    public async Task<GeolocationInfo?> GetLocationInfoAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        // IPs locales
        if (IsLocalIpAddress(ipAddress))
        {
            return CreateLocalGeolocationInfo(ipAddress);
        }

        // Cache check
        var cacheKey = $"geo_cf_{ipAddress}";
        try
        {
            var cached = await _cache.GetAsync<GeolocationInfo>(cacheKey);
            if (cached != null)
                return cached;
        }
        catch
        {
            // Cache falla no es crítico
        }

        // Crear objeto base
        var geoInfo = new GeolocationInfo { IpAddress = ipAddress };

        // 1. CLOUDFLARE PRIMERO (más confiable)
        ApplyCloudflareHeaders(geoInfo, ipAddress);

        // 2. MaxMind como fallback solo si Cloudflare no dio información completa
        if (ShouldUseMaxMindFallback(geoInfo) && _mmdbReader != null)
        {
            ApplyMaxMindFallback(geoInfo, ipAddress);
        }

        // 3. Aplicar valores por defecto inteligentes
        ApplyIntelligentDefaults(geoInfo);

        // Cache result
        try
        {
            await _cache.SetAsync(cacheKey, geoInfo, TimeSpan.FromHours(6));
        }
        catch
        {
            // Cache falla no es crítico
        }

        return geoInfo;
    }

    private void ApplyCloudflareHeaders(GeolocationInfo geoInfo, string ipAddress)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        var headers = httpContext.Request.Headers;

        // País
        var cfCountry = headers["CF-IPCountry"].ToString();
        if (!string.IsNullOrWhiteSpace(cfCountry) && cfCountry.Length == 2)
        {
            geoInfo.CountryCode = cfCountry.ToUpperInvariant();
            try
            {
                var regionInfo = new RegionInfo(geoInfo.CountryCode);
                geoInfo.Country = regionInfo.EnglishName;
            }
            catch
            {
                geoInfo.Country = geoInfo.CountryCode; // Fallback al código
            }
        }

        // Ciudad
        var cfCity = headers["CF-IPCity"].ToString();
        if (!string.IsNullOrWhiteSpace(cfCity))
        {
            geoInfo.City = Uri.UnescapeDataString(cfCity);
        }

        // Región/Estado
        var cfRegion = headers["CF-Region"].ToString();
        if (!string.IsNullOrWhiteSpace(cfRegion))
        {
            geoInfo.Region = cfRegion;
        }

        // Coordenadas
        var cfLatitude = headers["CF-IPLatitude"].ToString();
        var cfLongitude = headers["CF-IPLongitude"].ToString();

        if (
            double.TryParse(
                cfLatitude,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var lat
            )
        )
        {
            geoInfo.Latitude = lat;
        }

        if (
            double.TryParse(
                cfLongitude,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var lng
            )
        )
        {
            geoInfo.Longitude = lng;
        }

        // Si obtuvimos información de Cloudflare, loggear
        if (!string.IsNullOrEmpty(cfCountry))
        {
            _logger.LogDebug(
                "Cloudflare geolocation for {IpAddress}: {Country}, {City}",
                ipAddress,
                geoInfo.Country,
                geoInfo.City
            );
        }
    }

    private bool ShouldUseMaxMindFallback(GeolocationInfo geoInfo)
    {
        // Usar MaxMind solo si Cloudflare no proporcionó información básica
        return string.IsNullOrEmpty(geoInfo.Country)
            || (string.IsNullOrEmpty(geoInfo.City) && geoInfo.Latitude == null);
    }

    private void ApplyMaxMindFallback(GeolocationInfo geoInfo, string ipAddress)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out var parsedIp))
                return;

            var response = _mmdbReader!.City(parsedIp);

            // Solo llenar campos que Cloudflare no proporcionó
            if (string.IsNullOrEmpty(geoInfo.Country))
            {
                geoInfo.Country = response.Country?.Name;
                geoInfo.CountryCode = response.Country?.IsoCode;
            }

            if (string.IsNullOrEmpty(geoInfo.City))
            {
                geoInfo.City = response.City?.Name;
            }

            if (string.IsNullOrEmpty(geoInfo.Region))
            {
                geoInfo.Region = response.MostSpecificSubdivision?.Name;
            }

            if (geoInfo.Latitude == null && response.Location?.Latitude != null)
            {
                geoInfo.Latitude = response.Location.Latitude;
                geoInfo.Longitude = response.Location.Longitude;
            }

            if (string.IsNullOrEmpty(geoInfo.Timezone))
            {
                geoInfo.Timezone = response.Location?.TimeZone;
            }
        }
        catch (MaxMind.GeoIP2.Exceptions.AddressNotFoundException)
        {
            // IP no encontrada en MaxMind, normal
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MaxMind fallback failed for {IpAddress}", ipAddress);
        }
    }

    private void ApplyIntelligentDefaults(GeolocationInfo geoInfo)
    {
        // Timezone basado en país
        if (string.IsNullOrEmpty(geoInfo.Timezone) && !string.IsNullOrEmpty(geoInfo.CountryCode))
        {
            geoInfo.Timezone = GetTimezoneFromCountry(geoInfo.CountryCode);
        }

        // Coordenadas por defecto para países sin datos específicos
        if (geoInfo.Latitude == null && !string.IsNullOrEmpty(geoInfo.CountryCode))
        {
            var coords = GetDefaultCoordinatesForCountry(geoInfo.CountryCode);
            if (coords.HasValue)
            {
                geoInfo.Latitude = coords.Value.Lat;
                geoInfo.Longitude = coords.Value.Lng;

                // Si no tenemos ciudad, usar la capital
                if (string.IsNullOrEmpty(geoInfo.City))
                {
                    geoInfo.City = GetDefaultCityForCountry(geoInfo.CountryCode);
                }
            }
        }

        // Valores finales por defecto
        geoInfo.Country ??= "Unknown";
        geoInfo.CountryCode ??= "??";
        geoInfo.City ??= "Unknown";
        geoInfo.Region ??= "Unknown";
        geoInfo.Timezone ??= "UTC";
        geoInfo.ISP ??= "Unknown ISP";
    }

    private (double Lat, double Lng)? GetDefaultCoordinatesForCountry(string countryCode)
    {
        return countryCode.ToUpperInvariant() switch
        {
            "DO" => (18.4861, -69.9312), // Santo Domingo
            "US" => (39.8283, -98.5795), // Centro geográfico US
            "CA" => (56.1304, -106.3468), // Centro geográfico Canada
            "MX" => (23.6345, -102.5528), // Centro geográfico Mexico
            "BR" => (-14.2350, -51.9253), // Centro geográfico Brazil
            "AR" => (-38.4161, -63.6167), // Centro geográfico Argentina
            "CL" => (-35.6751, -71.5430), // Santiago
            "CO" => (4.5709, -74.2973), // Bogotá
            "PE" => (-9.1900, -75.0152), // Lima
            "CR" => (9.7489, -83.7534), // San José
            "PA" => (8.5380, -80.7821), // Panama City
            "GB" => (55.3781, -3.4360), // Centro UK
            "DE" => (51.1657, 10.4515), // Centro Germany
            "FR" => (46.6034, 1.8883), // Centro France
            "ES" => (40.4637, -3.7492), // Madrid
            "IT" => (41.8719, 12.5674), // Roma
            "NL" => (52.1326, 5.2913), // Centro Netherlands
            "JP" => (36.2048, 138.2529), // Centro Japan
            "AU" => (-25.2744, 133.7751), // Centro Australia
            _ => null,
        };
    }

    private string GetDefaultCityForCountry(string countryCode)
    {
        return countryCode.ToUpperInvariant() switch
        {
            "DO" => "Santo Domingo",
            "US" => "New York",
            "CA" => "Toronto",
            "MX" => "Mexico City",
            "BR" => "São Paulo",
            "AR" => "Buenos Aires",
            "CL" => "Santiago",
            "CO" => "Bogotá",
            "PE" => "Lima",
            "CR" => "San José",
            "PA" => "Panama City",
            "GB" => "London",
            "DE" => "Berlin",
            "FR" => "Paris",
            "ES" => "Madrid",
            "IT" => "Rome",
            "NL" => "Amsterdam",
            "JP" => "Tokyo",
            "AU" => "Sydney",
            _ => "Unknown",
        };
    }

    private string GetTimezoneFromCountry(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "DO" => "America/Santo_Domingo",
            "US" => "America/New_York",
            "CA" => "America/Toronto",
            "MX" => "America/Mexico_City",
            "BR" => "America/Sao_Paulo",
            "AR" => "America/Argentina/Buenos_Aires",
            "CL" => "America/Santiago",
            "CO" => "America/Bogota",
            "PE" => "America/Lima",
            "CR" => "America/Costa_Rica",
            "PA" => "America/Panama",
            "GB" => "Europe/London",
            "DE" => "Europe/Berlin",
            "FR" => "Europe/Paris",
            "ES" => "Europe/Madrid",
            "IT" => "Europe/Rome",
            "NL" => "Europe/Amsterdam",
            "PT" => "Europe/Lisbon",
            "PL" => "Europe/Warsaw",
            "RU" => "Europe/Moscow",
            "JP" => "Asia/Tokyo",
            "CN" => "Asia/Shanghai",
            "KR" => "Asia/Seoul",
            "IN" => "Asia/Kolkata",
            "TH" => "Asia/Bangkok",
            "SG" => "Asia/Singapore",
            "AU" => "Australia/Sydney",
            "NZ" => "Pacific/Auckland",
            _ => "UTC",
        };
    }

    public async Task<string> GetLocationDisplayAsync(string ipAddress)
    {
        var geoInfo = await GetLocationInfoAsync(ipAddress);

        if (geoInfo == null)
            return "Unknown Location";

        if (geoInfo.Country == "Local")
            return "Local Development";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(geoInfo.City) && geoInfo.City != "Unknown")
            parts.Add(geoInfo.City);

        if (
            !string.IsNullOrEmpty(geoInfo.Region)
            && geoInfo.Region != geoInfo.City
            && geoInfo.Region != "Unknown"
        )
            parts.Add(geoInfo.Region);

        if (!string.IsNullOrEmpty(geoInfo.Country) && geoInfo.Country != "Unknown")
            parts.Add(geoInfo.Country);

        var location = string.Join(", ", parts);
        return string.IsNullOrEmpty(location) ? "Unknown Location" : location;
    }

    private static GeolocationInfo CreateLocalGeolocationInfo(string ipAddress)
    {
        return new GeolocationInfo
        {
            IpAddress = ipAddress,
            Country = "Local",
            CountryCode = "LC",
            City = "Development",
            Region = "Local Development",
            Latitude = 0,
            Longitude = 0,
            Timezone = TimeZoneInfo.Local.Id,
            ISP = "Local Network",
        };
    }

    private static bool IsLocalIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return true;

        var localIndicators = new[] { "::1", "127.0.0.1", "localhost", "0.0.0.0", "unknown" };

        if (localIndicators.Contains(ipAddress, StringComparer.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(ipAddress, out var parsedIp))
            return true;

        var bytes = parsedIp.GetAddressBytes();
        if (bytes.Length == 4)
        {
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
            if (bytes[0] == 10)
                return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;
            if (bytes[0] == 127)
                return true;
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                return true;
        }

        return false;
    }

    public void Dispose()
    {
        _mmdbReader?.Dispose();
    }
}

public class GeolocationInfo
{
    public string IpAddress { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
    public string? ISP { get; set; }

    public string GetLocationKey()
    {
        if (Country == "Local")
            return "local-dev";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Country) && Country != "Unknown")
            parts.Add(Country.ToLowerInvariant());

        if (!string.IsNullOrEmpty(City) && City != "Unknown")
            parts.Add(City.ToLowerInvariant());

        return parts.Any() ? string.Join("-", parts) : "unknown";
    }

    public (string? Lat, string? Lng) GetCoordinatesAsString()
    {
        return (
            Latitude?.ToString("F6", CultureInfo.InvariantCulture),
            Longitude?.ToString("F6", CultureInfo.InvariantCulture)
        );
    }
}


// Esta podria ser la implementacion que podriamos usar si usamos un servicio free:

// using System.Net.Http;
// using System.Text.Json;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Configuration;

// namespace AuthService.Infraestructure.Services;

// public interface IGeolocationService
// {
//     Task<GeolocationInfo?> GetLocationInfoAsync(string ipAddress);
//     Task<string> GetLocationDisplayAsync(string ipAddress);
// }

// public class GeolocationService : IGeolocationService
// {
//     private readonly HttpClient _httpClient;
//     private readonly ILogger<GeolocationService> _logger;
//     private readonly string _apiKey;
//     private readonly Dictionary<string, GeolocationInfo> _cache;
//     private static readonly object _cacheLock = new();

//     public GeolocationService(
//         HttpClient httpClient,
//         ILogger<GeolocationService> logger,
//         IConfiguration configuration)
//     {
//         _httpClient = httpClient;
//         _logger = logger;
//         _apiKey = configuration.GetValue<string>("Geolocation:ApiKey") ?? "";
//         _cache = new Dictionary<string, GeolocationInfo>();
//     }

//     public async Task<GeolocationInfo?> GetLocationInfoAsync(string ipAddress)
//     {
//         if (string.IsNullOrWhiteSpace(ipAddress))
//             return null;

//         // Manejar IP local/localhost
//         if (IsLocalIpAddress(ipAddress))
//         {
//             return new GeolocationInfo
//             {
//                 IpAddress = ipAddress,
//                 Country = "Local",
//                 CountryCode = "LC",
//                 City = "Development",
//                 Region = "Local Development",
//                 Latitude = 0,
//                 Longitude = 0,
//                 Timezone = "UTC",
//                 ISP = "Local Network"
//             };
//         }

//         // Verificar cache
//         lock (_cacheLock)
//         {
//             if (_cache.TryGetValue(ipAddress, out var cached))
//             {
//                 return cached;
//             }
//         }

//         try
//         {
//             // Usar servicio gratuito ip-api.com (max 1000 requests/hour sin API key)
//             var url = $"http://ip-api.com/json/{ipAddress}?fields=status,message,country,countryCode,region,regionName,city,zip,lat,lon,timezone,isp,org,as,query";

//             var response = await _httpClient.GetAsync(url);
//             response.EnsureSuccessStatusCode();

//             var jsonContent = await response.Content.ReadAsStringAsync();
//             var apiResponse = JsonSerializer.Deserialize<IpApiResponse>(jsonContent, new JsonSerializerOptions
//             {
//                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//             });

//             if (apiResponse?.Status == "success")
//             {
//                 var geoInfo = new GeolocationInfo
//                 {
//                     IpAddress = ipAddress,
//                     Country = apiResponse.Country,
//                     CountryCode = apiResponse.CountryCode,
//                     City = apiResponse.City,
//                     Region = apiResponse.RegionName,
//                     Latitude = apiResponse.Lat,
//                     Longitude = apiResponse.Lon,
//                     Timezone = apiResponse.Timezone,
//                     ISP = apiResponse.Isp
//                 };

//                 // Cache the result
//                 lock (_cacheLock)
//                 {
//                     _cache[ipAddress] = geoInfo;
//                 }

//                 return geoInfo;
//             }
//             else
//             {
//                 _logger.LogWarning("Geolocation API returned error for IP {IpAddress}: {Message}",
//                     ipAddress, apiResponse?.Message);
//                 return null;
//             }
//         }
//         catch (HttpRequestException ex)
//         {
//             _logger.LogError(ex, "HTTP error getting location for IP {IpAddress}", ipAddress);
//             return null;
//         }
//         catch (JsonException ex)
//         {
//             _logger.LogError(ex, "JSON parsing error for IP {IpAddress}", ipAddress);
//             return null;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Unexpected error getting location for IP {IpAddress}", ipAddress);
//             return null;
//         }
//     }

//     public async Task<string> GetLocationDisplayAsync(string ipAddress)
//     {
//         var geoInfo = await GetLocationInfoAsync(ipAddress);

//         if (geoInfo == null)
//             return "Unknown Location";

//         if (geoInfo.Country == "Local")
//             return "🏠 Local Development";

//         var parts = new List<string>();

//         if (!string.IsNullOrEmpty(geoInfo.City))
//             parts.Add(geoInfo.City);

//         if (!string.IsNullOrEmpty(geoInfo.Region) && geoInfo.Region != geoInfo.City)
//             parts.Add(geoInfo.Region);

//         if (!string.IsNullOrEmpty(geoInfo.Country))
//             parts.Add(geoInfo.Country);

//         var location = string.Join(", ", parts);
//         return string.IsNullOrEmpty(location) ? "Unknown Location" : location;
//     }

//     private static bool IsLocalIpAddress(string ipAddress)
//     {
//         // Direcciones IP locales comunes
//         var localIps = new[]
//         {
//             "::1",           // IPv6 localhost
//             "127.0.0.1",     // IPv4 localhost
//             "localhost",     // hostname
//             "0.0.0.0"        // All interfaces
//         };

//         return localIps.Contains(ipAddress, StringComparer.OrdinalIgnoreCase) ||
//                ipAddress.StartsWith("192.168.") ||  // Private network
//                ipAddress.StartsWith("10.") ||       // Private network
//                ipAddress.StartsWith("172.");        // Private network range
//     }
// }

// // Modelos para la API de geolocalización
// public class GeolocationInfo
// {
//     public string IpAddress { get; set; } = string.Empty;
//     public string? Country { get; set; }
//     public string? CountryCode { get; set; }
//     public string? City { get; set; }
//     public string? Region { get; set; }
//     public double? Latitude { get; set; }
//     public double? Longitude { get; set; }
//     public string? Timezone { get; set; }
//     public string? ISP { get; set; }
// }

// public class IpApiResponse
// {
//     public string? Status { get; set; }
//     public string? Message { get; set; }
//     public string? Country { get; set; }
//     public string? CountryCode { get; set; }
//     public string? Region { get; set; }
//     public string? RegionName { get; set; }
//     public string? City { get; set; }
//     public string? Zip { get; set; }
//     public double Lat { get; set; }
//     public double Lon { get; set; }
//     public string? Timezone { get; set; }
//     public string? Isp { get; set; }
//     public string? Org { get; set; }
//     public string? As { get; set; }
//     public string? Query { get; set; }
// }
