namespace DUSK.Core;

/// <summary>
/// Cache provider abstraction for the 3-layer caching system.
/// L1: Memory (fastest, smallest)
/// L2: Redis (distributed, medium)
/// L3: MongoDB (persistent, largest)
/// </summary>
public interface ICacheProvider
{
    string Name { get; }
    CacheLayer Layer { get; }
    bool IsConnected { get; }

    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);

    Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken ct = default);
    Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions? options = null, CancellationToken ct = default);

    Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

public enum CacheLayer
{
    L1_Memory = 1,
    L2_Redis = 2,
    L3_MongoDB = 3
}

public record CacheEntryOptions
{
    public TimeSpan? AbsoluteExpiration { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
    public CachePriority Priority { get; init; } = CachePriority.Normal;
    public string[]? Tags { get; init; }

    public static CacheEntryOptions Default => new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(5),
        Priority = CachePriority.Normal
    };

    public static CacheEntryOptions LongLived => new()
    {
        AbsoluteExpiration = TimeSpan.FromHours(24),
        Priority = CachePriority.High
    };

    public static CacheEntryOptions ShortLived => new()
    {
        AbsoluteExpiration = TimeSpan.FromSeconds(30),
        Priority = CachePriority.Low
    };
}

public enum CachePriority
{
    Low,
    Normal,
    High,
    NeverRemove
}

public record CacheStatistics(
    CacheLayer Layer,
    long TotalItems,
    long TotalSizeBytes,
    long HitCount,
    long MissCount,
    double HitRate,
    DateTime LastAccess
);

/// <summary>
/// Orchestrates the 3-layer cache system.
/// Handles cache-through logic and synchronization between layers.
/// </summary>
public interface ICacheOrchestrator
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default);
    Task InvalidateAsync(string key, CancellationToken ct = default);
    Task InvalidateByTagAsync(string tag, CancellationToken ct = default);

    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken ct = default);

    Task WarmupAsync(IEnumerable<string> keys, CancellationToken ct = default);
    Task<CacheHealthReport> GetHealthReportAsync(CancellationToken ct = default);
}

public record CacheHealthReport(
    bool IsHealthy,
    CacheLayerHealth L1Health,
    CacheLayerHealth L2Health,
    CacheLayerHealth L3Health,
    DateTime GeneratedAt
);

public record CacheLayerHealth(
    CacheLayer Layer,
    bool IsConnected,
    TimeSpan Latency,
    CacheStatistics? Statistics,
    string? ErrorMessage
);
