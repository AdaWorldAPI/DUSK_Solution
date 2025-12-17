namespace DUSK.Sync.Providers;

using System.Collections.Concurrent;
using DUSK.Core;

/// <summary>
/// L1 Cache: In-memory cache for fastest access.
/// Uses ConcurrentDictionary for thread-safe operations.
/// Typical latency: &lt;1ms
/// </summary>
public sealed class L1_MemoryCache : ICacheProvider
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly int _maxSize;
    private readonly Timer _cleanupTimer;
    private long _hitCount;
    private long _missCount;
    private DateTime _lastAccess = DateTime.UtcNow;

    public string Name => "L1_Memory";
    public CacheLayer Layer => CacheLayer.L1_Memory;
    public bool IsConnected => true;

    public L1_MemoryCache(int maxSizeBytes = 100 * 1024 * 1024) // 100MB default
    {
        _maxSize = maxSizeBytes;
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _lastAccess = DateTime.UtcNow;

        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired)
            {
                Interlocked.Increment(ref _hitCount);
                entry.UpdateSlidingExpiration();
                return Task.FromResult((T?)entry.Value);
            }

            _cache.TryRemove(key, out _);
        }

        Interlocked.Increment(ref _missCount);
        return Task.FromResult(default(T?));
    }

    public Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        options ??= CacheEntryOptions.Default;
        var entry = new CacheEntry(value, options);
        _cache.AddOrUpdate(key, entry, (_, _) => entry);
        _lastAccess = DateTime.UtcNow;

        EnsureSizeLimit();
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var result = new Dictionary<string, T?>();
        foreach (var key in keys)
        {
            result[key] = await GetAsync<T>(key, ct);
        }
        return result;
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        foreach (var kvp in items)
        {
            await SetAsync(kvp.Key, kvp.Value, options, ct);
        }
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var totalSize = _cache.Values.Sum(e => e.ApproximateSize);
        var hitRate = _hitCount + _missCount > 0
            ? (double)_hitCount / (_hitCount + _missCount)
            : 0;

        return Task.FromResult(new CacheStatistics(
            Layer,
            _cache.Count,
            totalSize,
            _hitCount,
            _missCount,
            hitRate,
            _lastAccess
        ));
    }

    private void CleanupExpired(object? state)
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private void EnsureSizeLimit()
    {
        var currentSize = _cache.Values.Sum(e => e.ApproximateSize);
        if (currentSize <= _maxSize) return;

        var toRemove = _cache
            .OrderBy(kvp => kvp.Value.Priority)
            .ThenBy(kvp => kvp.Value.LastAccess)
            .Take(_cache.Count / 4)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private sealed class CacheEntry
    {
        public object? Value { get; }
        public DateTime CreatedAt { get; }
        public DateTime? AbsoluteExpiration { get; }
        public TimeSpan? SlidingExpiration { get; }
        public DateTime LastAccess { get; private set; }
        public CachePriority Priority { get; }
        public string[]? Tags { get; }

        public bool IsExpired =>
            (AbsoluteExpiration.HasValue && DateTime.UtcNow > AbsoluteExpiration) ||
            (SlidingExpiration.HasValue && DateTime.UtcNow > LastAccess.Add(SlidingExpiration.Value));

        public long ApproximateSize => Value switch
        {
            string s => s.Length * 2,
            byte[] b => b.Length,
            _ => 100 // rough estimate
        };

        public CacheEntry(object? value, CacheEntryOptions options)
        {
            Value = value;
            CreatedAt = DateTime.UtcNow;
            LastAccess = DateTime.UtcNow;
            Priority = options.Priority;
            Tags = options.Tags;

            if (options.AbsoluteExpiration.HasValue)
                AbsoluteExpiration = DateTime.UtcNow.Add(options.AbsoluteExpiration.Value);

            SlidingExpiration = options.SlidingExpiration;
        }

        public void UpdateSlidingExpiration()
        {
            LastAccess = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        _cache.Clear();
    }
}
