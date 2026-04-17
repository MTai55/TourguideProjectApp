using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace TourGuideAPI.Services;

/// <summary>
/// Cache service interface cho việc cache dữ liệu
/// </summary>
public interface ICacheService
{
    T? Get<T>(string key);
    Task<T?> GetAsync<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    void Remove(string key);
    Task RemoveAsync(string key);
    void RemoveByPattern(string pattern);
}

/// <summary>
/// In-memory cache implementation
/// </summary>
public class MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger) : ICacheService
{
    private static readonly ConcurrentDictionary<string, byte> _allKeys = new();
    private const string CACHE_PREFIX = "tour_";
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);

    public T? Get<T>(string key)
    {
        var cacheKey = $"{CACHE_PREFIX}{key}";
        return memoryCache.TryGetValue(cacheKey, out T? value) ? value : default;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult(Get<T>(key));
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var cacheKey = $"{CACHE_PREFIX}{key}";
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };
        
        memoryCache.Set(cacheKey, value, cacheOptions);
        _allKeys.TryAdd(cacheKey, 0);
        
        logger.LogInformation($"✅ Cache SET: {key} (expires in {(expiration ?? _defaultExpiration).TotalSeconds}s)");
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        Set(key, value, expiration);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        var cacheKey = $"{CACHE_PREFIX}{key}";
        memoryCache.Remove(cacheKey);
        _allKeys.TryRemove(cacheKey, out _);
        logger.LogInformation($"🗑️ Cache REMOVED: {key}");
    }

    public Task RemoveAsync(string key)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Xóa tất cả cache keys match pattern (wildcard)
    /// Ví dụ: "places:*" sẽ xóa "places:1", "places:2", v.v.
    /// </summary>
    public void RemoveByPattern(string pattern)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape($"{CACHE_PREFIX}{pattern}")
                .Replace("\\*", ".*") + "$");

        var keysToRemove = _allKeys.Keys
            .Where(k => regex.IsMatch(k))
            .ToList();

        foreach (var key in keysToRemove)
        {
            memoryCache.Remove(key);
            _allKeys.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
            logger.LogInformation($"🗑️ Cache REMOVED by pattern '{pattern}': {keysToRemove.Count} keys");
    }
}
