namespace DUSK.Sync;

using System.Diagnostics;
using DUSK.Core;
using DUSK.Sync.Providers;

// Note: Call DataPulseMonitor.Instance to enable waveform visualization

/// <summary>
/// Orchestrates the 3-layer cache system.
/// Read-through: L1 -> L2 -> L3 -> Source
/// Write-through: Write to all layers for consistency
/// </summary>
public sealed class CacheOrchestrator : ICacheOrchestrator, IDisposable
{
    private readonly ICacheProvider _l1;
    private readonly ICacheProvider _l2;
    private readonly ICacheProvider _l3;
    private readonly CacheOrchestratorOptions _options;

    public CacheOrchestrator(
        ICacheProvider l1,
        ICacheProvider l2,
        ICacheProvider l3,
        CacheOrchestratorOptions? options = null)
    {
        _l1 = l1;
        _l2 = l2;
        _l3 = l3;
        _options = options ?? new CacheOrchestratorOptions();
    }

    public static CacheOrchestrator CreateDefault(
        string? redisConnection = null,
        string? mongoConnection = null)
    {
        var l1 = new L1_MemoryCache();
        var l2 = new L2_RedisCache(redisConnection ?? "localhost:6379");
        var l3 = new L3_MongoCache(mongoConnection ?? "mongodb://localhost:27017");

        return new CacheOrchestrator(l1, l2, l3);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Try L1 (Memory)
        var result = await _l1.GetAsync<T>(key, ct);
        if (result != null)
        {
            sw.Stop();
            DataPulseMonitor.Instance.RecordHit(CacheLayerSource.L1_Memory, key, sw.Elapsed);
            return result;
        }

        // Try L2 (Redis)
        result = await _l2.GetAsync<T>(key, ct);
        if (result != null)
        {
            sw.Stop();
            DataPulseMonitor.Instance.RecordHit(CacheLayerSource.L2_Redis, key, sw.Elapsed);
            // Populate L1
            await _l1.SetAsync(key, result, CacheEntryOptions.ShortLived, ct);
            return result;
        }

        // Try L3 (MongoDB)
        result = await _l3.GetAsync<T>(key, ct);
        if (result != null)
        {
            sw.Stop();
            DataPulseMonitor.Instance.RecordHit(CacheLayerSource.L3_MongoDB, key, sw.Elapsed);
            // Populate L1 and L2
            var populateTasks = new[]
            {
                _l1.SetAsync(key, result, CacheEntryOptions.ShortLived, ct),
                _l2.SetAsync(key, result, CacheEntryOptions.Default, ct)
            };
            await Task.WhenAll(populateTasks);
            return result;
        }

        sw.Stop();
        DataPulseMonitor.Instance.RecordMiss(key, sw.Elapsed);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        options ??= CacheEntryOptions.Default;
        var sw = Stopwatch.StartNew();

        // Write to all layers based on strategy
        if (_options.WriteStrategy == WriteStrategy.WriteThrough)
        {
            var tasks = new[]
            {
                _l1.SetAsync(key, value, options with { AbsoluteExpiration = _options.L1DefaultTTL }, ct),
                _l2.SetAsync(key, value, options with { AbsoluteExpiration = _options.L2DefaultTTL }, ct),
                _l3.SetAsync(key, value, options with { AbsoluteExpiration = _options.L3DefaultTTL }, ct)
            };
            await Task.WhenAll(tasks);
            sw.Stop();
            DataPulseMonitor.Instance.RecordWrite(CacheLayerSource.L3_MongoDB, key, sw.Elapsed);
        }
        else // WriteBehind - async to L2/L3
        {
            await _l1.SetAsync(key, value, options with { AbsoluteExpiration = _options.L1DefaultTTL }, ct);
            sw.Stop();
            DataPulseMonitor.Instance.RecordWrite(CacheLayerSource.L1_Memory, key, sw.Elapsed);

            _ = Task.Run(async () =>
            {
                var sw2 = Stopwatch.StartNew();
                await _l2.SetAsync(key, value, options with { AbsoluteExpiration = _options.L2DefaultTTL }, CancellationToken.None);
                DataPulseMonitor.Instance.RecordWrite(CacheLayerSource.L2_Redis, key, sw2.Elapsed);
                await _l3.SetAsync(key, value, options with { AbsoluteExpiration = _options.L3DefaultTTL }, CancellationToken.None);
                DataPulseMonitor.Instance.RecordWrite(CacheLayerSource.L3_MongoDB, key, sw2.Elapsed);
            }, CancellationToken.None);
        }
    }

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        var tasks = new[]
        {
            _l1.RemoveAsync(key, ct),
            _l2.RemoveAsync(key, ct),
            _l3.RemoveAsync(key, ct)
        };
        await Task.WhenAll(tasks);
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        var tasks = new List<Task>
        {
            _l1.ClearAsync(ct) // L1 doesn't support tags, clear relevant items
        };

        if (_l2 is L2_RedisCache redis)
        {
            tasks.Add(redis.InvalidateByTagAsync(tag, ct));
        }

        if (_l3 is L3_MongoCache mongo)
        {
            tasks.Add(mongo.InvalidateByTagAsync(tag, ct));
        }

        await Task.WhenAll(tasks);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        var existing = await GetAsync<T>(key, ct);
        if (existing != null) return existing;

        var value = await factory(ct);
        await SetAsync(key, value, options, ct);
        return value;
    }

    public async Task WarmupAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var keyList = keys.ToList();

        // Get from L3 and populate upper layers
        var l3Values = await _l3.GetManyAsync<object>(keyList, ct);

        var l1Tasks = new List<Task>();
        var l2Tasks = new List<Task>();

        foreach (var kvp in l3Values.Where(kv => kv.Value != null))
        {
            l1Tasks.Add(_l1.SetAsync(kvp.Key, kvp.Value, CacheEntryOptions.ShortLived, ct));
            l2Tasks.Add(_l2.SetAsync(kvp.Key, kvp.Value, CacheEntryOptions.Default, ct));
        }

        await Task.WhenAll(l1Tasks.Concat(l2Tasks));
    }

    public async Task<CacheHealthReport> GetHealthReportAsync(CancellationToken ct = default)
    {
        var l1Health = await GetLayerHealthAsync(_l1, ct);
        var l2Health = await GetLayerHealthAsync(_l2, ct);
        var l3Health = await GetLayerHealthAsync(_l3, ct);

        var isHealthy = l1Health.IsConnected &&
                       (l2Health.IsConnected || !_options.RequireL2) &&
                       (l3Health.IsConnected || !_options.RequireL3);

        return new CacheHealthReport(isHealthy, l1Health, l2Health, l3Health, DateTime.UtcNow);
    }

    private async Task<CacheLayerHealth> GetLayerHealthAsync(ICacheProvider provider, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        CacheStatistics? stats = null;
        string? error = null;

        try
        {
            // Test connectivity with a simple operation
            await provider.ExistsAsync("__health_check__", ct);
            stats = await provider.GetStatisticsAsync(ct);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        sw.Stop();

        return new CacheLayerHealth(
            provider.Layer,
            provider.IsConnected && error == null,
            sw.Elapsed,
            stats,
            error
        );
    }

    public void Dispose()
    {
        (_l1 as IDisposable)?.Dispose();
        (_l2 as IDisposable)?.Dispose();
        (_l3 as IDisposable)?.Dispose();
    }
}

public record CacheOrchestratorOptions
{
    public WriteStrategy WriteStrategy { get; init; } = WriteStrategy.WriteThrough;
    public TimeSpan L1DefaultTTL { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan L2DefaultTTL { get; init; } = TimeSpan.FromHours(1);
    public TimeSpan L3DefaultTTL { get; init; } = TimeSpan.FromDays(7);
    public bool RequireL2 { get; init; } = false;
    public bool RequireL3 { get; init; } = false;
}

public enum WriteStrategy
{
    WriteThrough,
    WriteBehind
}
