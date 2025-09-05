using System.Globalization;
using System.Net;
using AuthService.Infraestructure.Services;
using MaxMind.GeoIP2;
using SharedLibrary.Caching;

namespace AuthService.Applications.Services;

public class GeolocationService : IGeolocationService, IDisposable
{
    private readonly ILogger<GeolocationService> _logger;
    private readonly IHybridCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DatabaseReader? _mmdbReader;
    private readonly bool _isMaxMindAvailable;

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

        // Intentar cargar MaxMind DB con múltiples rutas posibles
        _mmdbReader = InitializeMaxMindDatabase();
        _isMaxMindAvailable = _mmdbReader != null;

        _logger.LogInformation(
            "GeolocationService initialized. MaxMind available: {MaxMindAvailable}",
            _isMaxMindAvailable
        );
    }

    private DatabaseReader? InitializeMaxMindDatabase()
    {
        var possiblePaths = new[]
        {
            _configuration["GeoIP:MmdbPath"], // Configuración principal
            "/Data/GeoLite2-City.mmdb", // Docker mount
            "/app/Data/GeoLite2-City.mmdb", // Alternativa Docker
            "./Data/GeoLite2-City.mmdb", // Desarrollo local
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "GeoLite2-City.mmdb"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GeoLite2-City.mmdb"),
        };

        foreach (var path in possiblePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            try
            {
                if (File.Exists(path))
                {
                    var reader = new DatabaseReader(path);
                    _logger.LogInformation(
                        "MaxMind GeoIP database loaded successfully from: {Path}",
                        path
                    );
                    return reader;
                }
                else
                {
                    _logger.LogDebug("MaxMind database not found at: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load MaxMind database from: {Path}", path);
            }
        }

        _logger.LogWarning(
            "MaxMind GeoIP database could not be loaded from any path. Falling back to basic geolocation."
        );
        return null;
    }

    public async Task<GeolocationInfo?> GetLocationInfoAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogDebug("Empty IP address provided");
            return null;
        }

        // Detectar IPs locales
        if (IsLocalIpAddress(ipAddress))
        {
            _logger.LogDebug("Local IP detected: {IpAddress}", ipAddress);
            return CreateLocalGeolocationInfo(ipAddress);
        }

        // Verificar caché con Redis
        var cacheKey = $"geo_v2_{ipAddress}";
        try
        {
            var cached = await _cache.GetAsync<GeolocationInfo>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Geolocation cache hit for IP: {IpAddress}", ipAddress);
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from cache for IP: {IpAddress}", ipAddress);
            // Continuar sin caché si falla
        }

        GeolocationInfo? geoInfo = null;

        // 1. Intentar MaxMind primero (más preciso)
        if (_isMaxMindAvailable && _mmdbReader != null)
        {
            geoInfo = GetLocationFromMaxMind(ipAddress);
        }

        // 2. Si MaxMind falla, usar métodos alternativos
        if (geoInfo == null)
        {
            geoInfo = await GetLocationFromAlternativeMethodsAsync(ipAddress);
        }

        // 3. Enriquecer con headers de Cloudflare si están disponibles
        EnrichWithCloudflareHeaders(geoInfo, ipAddress);

        // 4. Aplicar valores por defecto si aún faltan datos
        ApplyDefaultValues(geoInfo, ipAddress);

        // 5. Guardar en caché con Redis
        if (geoInfo != null)
        {
            try
            {
                await _cache.SetAsync(cacheKey, geoInfo, TimeSpan.FromHours(6));
                _logger.LogDebug(
                    "Geolocation cached for IP: {IpAddress} - {Location}",
                    ipAddress,
                    $"{geoInfo.City}, {geoInfo.Country}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching geolocation for IP: {IpAddress}", ipAddress);
                // No falla si no puede cachear
            }
        }

        return geoInfo;
    }

    private GeolocationInfo? GetLocationFromMaxMind(string ipAddress)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out var parsedIp))
            {
                _logger.LogWarning(
                    "Invalid IP address format for MaxMind lookup: {IpAddress}",
                    ipAddress
                );
                return null;
            }

            var response = _mmdbReader!.City(parsedIp);

            var geoInfo = new GeolocationInfo
            {
                IpAddress = ipAddress,
                Country = response.Country?.Name,
                CountryCode = response.Country?.IsoCode,
                City = response.City?.Name,
                Region = response.MostSpecificSubdivision?.Name,
                Latitude = response.Location?.Latitude,
                Longitude = response.Location?.Longitude,
                Timezone = response.Location?.TimeZone,
                ISP = null, // MaxMind City no incluye ISP
            };

            _logger.LogDebug(
                "MaxMind lookup successful for {IpAddress}: {City}, {Country}",
                ipAddress,
                geoInfo.City,
                geoInfo.Country
            );

            return geoInfo;
        }
        catch (MaxMind.GeoIP2.Exceptions.AddressNotFoundException)
        {
            _logger.LogDebug("IP address not found in MaxMind database: {IpAddress}", ipAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MaxMind lookup failed for IP: {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<GeolocationInfo?> GetLocationFromAlternativeMethodsAsync(string ipAddress)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out var parsedIp))
            {
                return null;
            }

            // Usar DNS reverse lookup para obtener información del hostname
            var hostEntry = await Dns.GetHostEntryAsync(parsedIp);
            var hostname = hostEntry.HostName?.ToLowerInvariant();

            var geoInfo = new GeolocationInfo { IpAddress = ipAddress, ISP = hostname };

            // Intentar extraer información geográfica del hostname
            if (!string.IsNullOrEmpty(hostname))
            {
                ExtractLocationFromHostname(hostname, geoInfo);
            }

            // Aplicar rangos de IP conocidos
            ApplyKnownIpRanges(parsedIp, geoInfo);

            _logger.LogDebug(
                "Alternative geolocation for {IpAddress}: {City}, {Country} (from hostname: {Hostname})",
                ipAddress,
                geoInfo.City,
                geoInfo.Country,
                hostname
            );

            return geoInfo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alternative geolocation failed for IP: {IpAddress}", ipAddress);
            return new GeolocationInfo { IpAddress = ipAddress };
        }
    }

    private void ExtractLocationFromHostname(string hostname, GeolocationInfo geoInfo)
    {
        var parts = hostname.Split('.');

        // Mapeos mejorados de códigos de país
        var countryMappings = new Dictionary<string, (string Country, string Code)>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            // América
            { "us", ("United States", "US") },
            { "usa", ("United States", "US") },
            { "ca", ("Canada", "CA") },
            { "mx", ("Mexico", "MX") },
            { "br", ("Brazil", "BR") },
            { "ar", ("Argentina", "AR") },
            { "cl", ("Chile", "CL") },
            { "co", ("Colombia", "CO") },
            { "pe", ("Peru", "PE") },
            { "do", ("Dominican Republic", "DO") },
            { "cr", ("Costa Rica", "CR") },
            { "pa", ("Panama", "PA") },
            // Europa
            { "uk", ("United Kingdom", "GB") },
            { "gb", ("United Kingdom", "GB") },
            { "de", ("Germany", "DE") },
            { "fr", ("France", "FR") },
            { "es", ("Spain", "ES") },
            { "it", ("Italy", "IT") },
            { "nl", ("Netherlands", "NL") },
            { "pt", ("Portugal", "PT") },
            { "pl", ("Poland", "PL") },
            { "ru", ("Russia", "RU") },
            // Asia
            { "jp", ("Japan", "JP") },
            { "cn", ("China", "CN") },
            { "kr", ("South Korea", "KR") },
            { "in", ("India", "IN") },
            { "th", ("Thailand", "TH") },
            { "sg", ("Singapore", "SG") },
            // Oceanía
            { "au", ("Australia", "AU") },
            { "nz", ("New Zealand", "NZ") },
        };

        // Mapeos de ciudades
        var cityMappings = new Dictionary<string, (string City, string Country, string Code)>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            // Estados Unidos
            { "nyc", ("New York", "United States", "US") },
            { "ny", ("New York", "United States", "US") },
            { "mia", ("Miami", "United States", "US") },
            { "miami", ("Miami", "United States", "US") },
            { "la", ("Los Angeles", "United States", "US") },
            { "sf", ("San Francisco", "United States", "US") },
            { "chi", ("Chicago", "United States", "US") },
            { "chicago", ("Chicago", "United States", "US") },
            { "dal", ("Dallas", "United States", "US") },
            { "dallas", ("Dallas", "United States", "US") },
            { "sea", ("Seattle", "United States", "US") },
            { "seattle", ("Seattle", "United States", "US") },
            { "bos", ("Boston", "United States", "US") },
            { "boston", ("Boston", "United States", "US") },
            { "atl", ("Atlanta", "United States", "US") },
            { "atlanta", ("Atlanta", "United States", "US") },
            { "den", ("Denver", "United States", "US") },
            { "denver", ("Denver", "United States", "US") },
            // Internacional
            { "lon", ("London", "United Kingdom", "GB") },
            { "london", ("London", "United Kingdom", "GB") },
            { "par", ("Paris", "France", "FR") },
            { "paris", ("Paris", "France", "FR") },
            { "ams", ("Amsterdam", "Netherlands", "NL") },
            { "amsterdam", ("Amsterdam", "Netherlands", "NL") },
            { "fra", ("Frankfurt", "Germany", "DE") },
            { "frankfurt", ("Frankfurt", "Germany", "DE") },
            { "tok", ("Tokyo", "Japan", "JP") },
            { "tokyo", ("Tokyo", "Japan", "JP") },
            { "syd", ("Sydney", "Australia", "AU") },
            { "sydney", ("Sydney", "Australia", "AU") },
        };

        foreach (var part in parts)
        {
            // Buscar ciudades primero (más específico)
            if (cityMappings.TryGetValue(part, out var cityInfo))
            {
                geoInfo.City = cityInfo.City;
                geoInfo.Country = cityInfo.Country;
                geoInfo.CountryCode = cityInfo.Code;
                continue;
            }

            // Buscar países
            if (countryMappings.TryGetValue(part, out var countryInfo))
            {
                geoInfo.Country ??= countryInfo.Country;
                geoInfo.CountryCode ??= countryInfo.Code;
            }
        }

        // Detectar proveedores ISP conocidos con ubicaciones probables
        var hostnameText = hostname.ToLowerInvariant();
        if (geoInfo.Country == null)
        {
            if (
                hostnameText.Contains("comcast")
                || hostnameText.Contains("verizon")
                || hostnameText.Contains("att")
                || hostnameText.Contains("charter")
            )
            {
                geoInfo.Country = "United States";
                geoInfo.CountryCode = "US";
            }
            else if (hostnameText.Contains("bt.") || hostnameText.Contains("virgin"))
            {
                geoInfo.Country = "United Kingdom";
                geoInfo.CountryCode = "GB";
            }
            else if (hostnameText.Contains("telefonica") || hostnameText.Contains("movistar"))
            {
                geoInfo.Country = "Spain";
                geoInfo.CountryCode = "ES";
            }
        }
    }

    private void ApplyKnownIpRanges(IPAddress ipAddress, GeolocationInfo geoInfo)
    {
        if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return; // Solo IPv4 por simplicidad

        var bytes = ipAddress.GetAddressBytes();
        var ipInt = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);

        var knownRanges = new[]
        {
            // Google DNS y servicios
            new
            {
                Start = "8.8.8.0",
                End = "8.8.8.255",
                Country = "United States",
                Code = "US",
                City = "Mountain View",
                Region = "California",
                Lat = 37.4056,
                Lng = -122.0775,
            },
            new
            {
                Start = "8.8.4.0",
                End = "8.8.4.255",
                Country = "United States",
                Code = "US",
                City = "Mountain View",
                Region = "California",
                Lat = 37.4056,
                Lng = -122.0775,
            },
            // Cloudflare
            new
            {
                Start = "1.1.1.0",
                End = "1.1.1.255",
                Country = "United States",
                Code = "US",
                City = "San Francisco",
                Region = "California",
                Lat = 37.7749,
                Lng = -122.4194,
            },
            new
            {
                Start = "1.0.0.0",
                End = "1.0.0.255",
                Country = "Australia",
                Code = "AU",
                City = "Sydney",
                Region = "New South Wales",
                Lat = -33.8688,
                Lng = 151.2093,
            },
            // OpenDNS
            new
            {
                Start = "208.67.222.0",
                End = "208.67.222.255",
                Country = "United States",
                Code = "US",
                City = "San Francisco",
                Region = "California",
                Lat = 37.7749,
                Lng = -122.4194,
            },
            new
            {
                Start = "208.67.220.0",
                End = "208.67.220.255",
                Country = "United States",
                Code = "US",
                City = "San Francisco",
                Region = "California",
                Lat = 37.7749,
                Lng = -122.4194,
            },
        };

        foreach (var range in knownRanges)
        {
            var startIp = IpToUint(range.Start);
            var endIp = IpToUint(range.End);

            if (ipInt >= startIp && ipInt <= endIp)
            {
                geoInfo.Country ??= range.Country;
                geoInfo.CountryCode ??= range.Code;
                geoInfo.City ??= range.City;
                geoInfo.Region ??= range.Region;
                geoInfo.Latitude ??= range.Lat;
                geoInfo.Longitude ??= range.Lng;
                break;
            }
        }
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

    private void EnrichWithCloudflareHeaders(GeolocationInfo? geoInfo, string ipAddress)
    {
        if (geoInfo == null)
            return;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        // Headers de Cloudflare
        var cfCountry = httpContext.Request.Headers["CF-IPCountry"].ToString();
        var cfCity = httpContext.Request.Headers["CF-IPCity"].ToString();
        var cfRegion = httpContext.Request.Headers["CF-Region"].ToString();
        var cfLatitude = httpContext.Request.Headers["CF-IPLatitude"].ToString();
        var cfLongitude = httpContext.Request.Headers["CF-IPLongitude"].ToString();

        // Aplicar información de Cloudflare si está disponible y más específica
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
                // Si no se puede obtener el nombre del país, mantener el código
            }
        }

        if (!string.IsNullOrWhiteSpace(cfCity))
        {
            geoInfo.City = Uri.UnescapeDataString(cfCity);
        }

        if (!string.IsNullOrWhiteSpace(cfRegion))
        {
            geoInfo.Region = cfRegion;
        }

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

        if (!string.IsNullOrEmpty(cfCountry))
        {
            _logger.LogDebug(
                "Enriched geolocation with Cloudflare headers for {IpAddress}: {City}, {Country}",
                ipAddress,
                geoInfo.City,
                geoInfo.Country
            );
        }
    }

    private void ApplyDefaultValues(GeolocationInfo? geoInfo, string ipAddress)
    {
        if (geoInfo == null)
            return;

        // Aplicar timezone basado en el país si no está disponible
        if (string.IsNullOrEmpty(geoInfo.Timezone) && !string.IsNullOrEmpty(geoInfo.CountryCode))
        {
            geoInfo.Timezone = GetTimezoneFromCountry(geoInfo.CountryCode);
        }

        // Valores por defecto para campos vacíos
        geoInfo.Country ??= "Unknown";
        geoInfo.CountryCode ??= "??";
        geoInfo.City ??= "Unknown";
        geoInfo.Region ??= "Unknown";
        geoInfo.Timezone ??= "UTC";
        geoInfo.ISP ??= "Unknown ISP";
    }

    private string GetTimezoneFromCountry(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "US" => "America/New_York",
            "CA" => "America/Toronto",
            "MX" => "America/Mexico_City",
            "BR" => "America/Sao_Paulo",
            "AR" => "America/Argentina/Buenos_Aires",
            "CL" => "America/Santiago",
            "CO" => "America/Bogota",
            "PE" => "America/Lima",
            "DO" => "America/Santo_Domingo",
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
            return "🏠 Local Development";

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
            return true; // IP inválida, tratar como local

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

            // 127.x.x.x (loopback)
            if (bytes[0] == 127)
                return true;

            // 169.254.x.x (link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // 100.64.x.x to 100.127.x.x (CGNAT)
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

    /// <summary>
    /// Genera una clave única de ubicación para comparaciones
    /// </summary>
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

    /// <summary>
    /// Obtiene coordenadas como string para almacenamiento en BD
    /// </summary>
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
