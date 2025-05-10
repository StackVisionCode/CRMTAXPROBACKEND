using Microsoft.Extensions.Caching.Memory;

public class TokenStorage : ITokenStorage
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenStorage> _logger;
    public TokenStorage(IMemoryCache cache, ILogger<TokenStorage> logger)
    {
        _logger = logger;
        _cache = cache;
    }


    public void StoreToken(AuthEventRequest authData)
    {
        try
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(authData.Expired);
            _cache.Set($"auth_token_{authData.data}", authData.data, authData.Expired);
             _logger.LogInformation($"Token stored for user {authData.data}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing token");
            throw;
        }
    }

    public string? GetToken(AuthEventRequest authData)
    {
        return _cache.Get<string>($"auth_token_{authData.data}");
    }

    public bool IsTokenValid(AuthEventRequest authData)
    {
        var token = GetToken(authData);
        return !string.IsNullOrEmpty(token);
    }
}