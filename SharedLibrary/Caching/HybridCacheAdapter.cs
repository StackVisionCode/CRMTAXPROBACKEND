using Microsoft.Extensions.Caching.Memory;

namespace SharedLibrary.Caching;

/// <summary>
/// Adaptador para mantener compatibilidad con IMemoryCache existente
/// </summary>
public sealed class HybridCacheAdapter : IMemoryCache
{
    private readonly IHybridCache _hybridCache;
    private readonly IMemoryCache _fallbackCache;

    public HybridCacheAdapter(IHybridCache hybridCache, IMemoryCache fallbackCache)
    {
        _hybridCache = hybridCache;
        _fallbackCache = fallbackCache;
    }

    public bool TryGetValue(object key, out object? value)
    {
        try
        {
            var keyStr = key.ToString() ?? throw new ArgumentNullException(nameof(key));
            var result = _hybridCache.GetAsync<object>(keyStr).GetAwaiter().GetResult();

            if (result != null)
            {
                value = result;
                return true;
            }
        }
        catch
        {
            // Fallback al caché original
            return _fallbackCache.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }

    public ICacheEntry CreateEntry(object key)
    {
        return _fallbackCache.CreateEntry(key);
    }

    public void Remove(object key)
    {
        var keyStr = key.ToString();
        if (keyStr != null)
        {
            _ = _hybridCache.RemoveAsync(keyStr);
        }
        _fallbackCache.Remove(key);
    }

    public void Dispose()
    {
        _fallbackCache.Dispose();
        if (_hybridCache is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
