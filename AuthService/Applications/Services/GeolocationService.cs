using System.Globalization;
using System.Net;
using AuthService.Infraestructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace AuthService.Applications.Services;

public class GeolocationService : IGeolocationService
{
    private readonly ILogger<GeolocationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _http;

    public GeolocationService(
        ILogger<GeolocationService> logger,
        IMemoryCache cache,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
        _http = httpContextAccessor;
    }

    public async Task<GeolocationInfo?> GetLocationInfoAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        // IP local => “Local Development”
        if (IsLocalIpAddress(ipAddress))
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

        // CACHE
        var cacheKey = $"geo_{ipAddress}";
        if (_cache.TryGetValue(cacheKey, out GeolocationInfo? cached))
            return cached;

        // "Hint" de país desde Cloudflare
        string? cfCountry = _http.HttpContext?.Request.Headers["CF-IPCountry"].ToString();
        cfCountry = string.IsNullOrWhiteSpace(cfCountry)
            ? null
            : cfCountry.Trim().ToUpperInvariant();

        var geoInfo = await GetLocationUsingDotNet(ipAddress);

        // Si Cloudflare nos da país, úsalo/ajústalo
        if (!string.IsNullOrEmpty(cfCountry) && cfCountry.Length == 2)
        {
            geoInfo ??= new GeolocationInfo { IpAddress = ipAddress };
            geoInfo.CountryCode = cfCountry;

            try
            {
                var ri = new RegionInfo(cfCountry);
                geoInfo.Country = ri.EnglishName;
            }
            catch
            {
                geoInfo.Country ??= cfCountry;
            }

            if (string.IsNullOrEmpty(geoInfo.Timezone))
                geoInfo.Timezone = GetTimezoneFromCountry(cfCountry); // <- generaliza
        }

        // Cachear 1 hora
        if (geoInfo != null)
        {
            _cache.Set(
                cacheKey,
                geoInfo,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                }
            );
        }

        return geoInfo;
    }

    private async Task<GeolocationInfo?> GetLocationUsingDotNet(string ipAddress)
    {
        try
        {
            // Validar que es una IP válida
            if (!IPAddress.TryParse(ipAddress, out var parsedIp))
            {
                _logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
                return null;
            }

            // Usar DNS lookup para obtener información básica
            var hostEntry = await Dns.GetHostEntryAsync(parsedIp);

            // Determinar región basada en el hostname si está disponible
            var locationInfo = new GeolocationInfo
            {
                IpAddress = ipAddress,
                ISP = hostEntry.HostName,
            };

            // Intentar determinar país/región basado en el hostname
            var hostnameParts = hostEntry.HostName.ToLower().Split('.');
            var countryInfo = ExtractLocationFromHostname(hostnameParts);

            locationInfo.Country = countryInfo.Country;
            locationInfo.CountryCode = countryInfo.CountryCode;
            locationInfo.City = countryInfo.City;
            locationInfo.Region = countryInfo.Region;

            // Para IPs públicas, intentar usar rangos de IP conocidos
            if (IsPublicIpAddress(parsedIp))
            {
                var ipRangeInfo = GetLocationFromIpRange(parsedIp);
                if (ipRangeInfo != null)
                {
                    locationInfo.Country = ipRangeInfo.Country ?? locationInfo.Country;
                    locationInfo.CountryCode = ipRangeInfo.CountryCode ?? locationInfo.CountryCode;
                    locationInfo.City = ipRangeInfo.City ?? locationInfo.City;
                    locationInfo.Region = ipRangeInfo.Region ?? locationInfo.Region;
                    locationInfo.Latitude = ipRangeInfo.Latitude;
                    locationInfo.Longitude = ipRangeInfo.Longitude;
                }
            }

            // Establecer timezone basado en el país
            locationInfo.Timezone = GetTimezoneFromCountry(locationInfo.CountryCode);

            _logger.LogDebug(
                "Resolved location for IP {IpAddress}: {Location}",
                ipAddress,
                $"{locationInfo.City}, {locationInfo.Country}"
            );

            return locationInfo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve hostname for IP {IpAddress}", ipAddress);

            // Fallback: información mínima
            return new GeolocationInfo
            {
                IpAddress = ipAddress,
                Country = "Unknown",
                CountryCode = "??",
                City = "Unknown",
                Region = "Unknown",
                Timezone = "UTC",
                ISP = "Unknown ISP",
            };
        }
    }

    private (
        string? Country,
        string? CountryCode,
        string? City,
        string? Region
    ) ExtractLocationFromHostname(string[] hostnameParts)
    {
        // Mapeo común de códigos de país en hostnames
        var countryMappings = new Dictionary<string, (string Country, string Code)>
        {
            { "us", ("United States", "US") },
            { "usa", ("United States", "US") },
            { "ca", ("Canada", "CA") },
            { "uk", ("United Kingdom", "GB") },
            { "de", ("Germany", "DE") },
            { "fr", ("France", "FR") },
            { "es", ("Spain", "ES") },
            { "it", ("Italy", "IT") },
            { "jp", ("Japan", "JP") },
            { "au", ("Australia", "AU") },
            { "br", ("Brazil", "BR") },
            { "mx", ("Mexico", "MX") },
            { "ar", ("Argentina", "AR") },
            { "co", ("Colombia", "CO") },
            { "cl", ("Chile", "CL") },
            { "pe", ("Peru", "PE") },
            { "do", ("Dominican Republic", "DO") },
        };

        // Mapeo de ciudades comunes
        var cityMappings = new Dictionary<string, string>
        {
            { "mia", "Miami" },
            { "miami", "Miami" },
            { "nyc", "New York" },
            { "ny", "New York" },
            { "la", "Los Angeles" },
            { "sf", "San Francisco" },
            { "chi", "Chicago" },
            { "dal", "Dallas" },
            { "den", "Denver" },
            { "sea", "Seattle" },
            { "bos", "Boston" },
            { "atl", "Atlanta" },
        };

        string? country = null;
        string? countryCode = null;
        string? city = null;
        string? region = null;

        foreach (var part in hostnameParts)
        {
            // Buscar código de país
            if (countryMappings.TryGetValue(part, out var countryInfo))
            {
                country = countryInfo.Country;
                countryCode = countryInfo.Code;
            }

            // Buscar ciudad
            if (cityMappings.TryGetValue(part, out var cityName))
            {
                city = cityName;
            }

            // Detectar proveedores ISP comunes
            if (part.Contains("comcast") || part.Contains("verizon") || part.Contains("att"))
            {
                country ??= "United States";
                countryCode ??= "US";
            }
        }

        // Si no encontramos país, asumir US para IPs corporativas comunes
        if (
            country == null
            && hostnameParts.Any(p => p.Contains("com") || p.Contains("net") || p.Contains("org"))
        )
        {
            country = "United States";
            countryCode = "US";
        }

        return (country, countryCode, city, region);
    }

    private GeolocationInfo? GetLocationFromIpRange(IPAddress ipAddress)
    {
        // Rangos conocidos de IP para algunos países/regiones
        // Esta es una implementación básica - en producción podrías usar una base de datos GeoIP

        var ipBytes = ipAddress.GetAddressBytes();
        if (ipBytes.Length != 4)
            return null; // Solo IPv4 por simplicidad

        var ipInt = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0);

        // Algunos rangos conocidos (ejemplos básicos)
        var ipRanges = new[]
        {
            // Estados Unidos - rangos aproximados
            new
            {
                StartIp = IpToUint("8.0.0.0"),
                EndIp = IpToUint("8.255.255.255"),
                Country = "United States",
                Code = "US",
                City = "Mountain View",
                Region = "California",
                Lat = 37.4056,
                Lng = -122.0775,
            },
            // Google DNS
            new
            {
                StartIp = IpToUint("8.8.8.0"),
                EndIp = IpToUint("8.8.8.255"),
                Country = "United States",
                Code = "US",
                City = "Mountain View",
                Region = "California",
                Lat = 37.4056,
                Lng = -122.0775,
            },
            // Cloudflare DNS
            new
            {
                StartIp = IpToUint("1.1.1.0"),
                EndIp = IpToUint("1.1.1.255"),
                Country = "United States",
                Code = "US",
                City = "San Francisco",
                Region = "California",
                Lat = 37.7749,
                Lng = -122.4194,
            },
        };

        foreach (var range in ipRanges)
        {
            if (ipInt >= range.StartIp && ipInt <= range.EndIp)
            {
                return new GeolocationInfo
                {
                    IpAddress = ipAddress.ToString(),
                    Country = range.Country,
                    CountryCode = range.Code,
                    City = range.City,
                    Region = range.Region,
                    Latitude = range.Lat,
                    Longitude = range.Lng,
                    Timezone = GetTimezoneFromCountry(range.Code),
                    ISP = "Known Range",
                };
            }
        }

        return null;
    }

    private uint IpToUint(string ipString)
    {
        if (IPAddress.TryParse(ipString, out var ip))
        {
            var bytes = ip.GetAddressBytes();
            return BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        }
        return 0;
    }

    private string GetTimezoneFromCountry(string? countryCode)
    {
        return countryCode?.ToUpper() switch
        {
            "US" => "America/New_York",
            "CA" => "America/Toronto",
            "GB" => "Europe/London",
            "DE" => "Europe/Berlin",
            "FR" => "Europe/Paris",
            "ES" => "Europe/Madrid",
            "IT" => "Europe/Rome",
            "JP" => "Asia/Tokyo",
            "AU" => "Australia/Sydney",
            "BR" => "America/Sao_Paulo",
            "MX" => "America/Mexico_City",
            "AR" => "America/Argentina/Buenos_Aires",
            "CO" => "America/Bogota",
            "CL" => "America/Santiago",
            "PE" => "America/Lima",
            "DO" => "America/Santo_Domingo",
            _ => "UTC",
        };
    }

    public async Task<string> GetLocationDisplayAsync(string ipAddress)
    {
        var geoInfo = await GetLocationInfoAsync(ipAddress);

        if (geoInfo == null)
            return "Unknown Location";

        if (geoInfo.Country == "Local")
            return "🏠 Local Development";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(geoInfo.City))
            parts.Add(geoInfo.City);

        if (!string.IsNullOrEmpty(geoInfo.Region) && geoInfo.Region != geoInfo.City)
            parts.Add(geoInfo.Region);

        if (!string.IsNullOrEmpty(geoInfo.Country))
            parts.Add(geoInfo.Country);

        var location = string.Join(", ", parts);
        return string.IsNullOrEmpty(location) ? "Unknown Location" : location;
    }

    private static bool IsLocalIpAddress(string ipAddress)
    {
        // Direcciones IP locales comunes
        var localIps = new[]
        {
            "::1", // IPv6 localhost
            "127.0.0.1", // IPv4 localhost
            "localhost", // hostname
            "0.0.0.0", // All interfaces
        };

        if (localIps.Contains(ipAddress, StringComparer.OrdinalIgnoreCase))
            return true;

        // Rangos privados
        if (IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            var bytes = parsedIp.GetAddressBytes();
            if (bytes.Length == 4) // IPv4
            {
                // 192.168.x.x
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;

                // 10.x.x.x
                if (bytes[0] == 10)
                    return true;

                // 172.16.x.x to 172.31.x.x
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return true;
            }
        }

        return false;
    }

    private static bool IsPublicIpAddress(IPAddress ipAddress)
    {
        return !IsLocalIpAddress(ipAddress.ToString());
    }
}

// Modelos para la información de geolocalización
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
