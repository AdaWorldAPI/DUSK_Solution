namespace DUSK.Sync.Providers;

using System.Text.Json;
using DUSK.Core;
using StackExchange.Redis;

/// <summary>
/// L2 Cache: Redis distributed cache for shared state across instances.
/// Typical latency: 1-5ms
/// </summary>
public sealed class L2_RedisCache : ICacheProvider, IDisposable
{
    private ConnectionMultiplexer? _connection;
    private IDatabase? _db;
    private readonly string _connectionString;
    private readonly string _keyPrefix;
    private readonly JsonSerializerOptions _jsonOptions;
    private long _hitCount;
    private long _missCount;
    private DateTime _lastAccess = DateTime.UtcNow;

    public string Name => "L2_Redis";
    public CacheLayer Layer => CacheLayer.L2_Redis;
    public bool IsConnected => _connection?.IsConnected ?? false;

    public L2_RedisCache(string connectionString = "localhost:6379", string keyPrefix = "dusk:")
    {
        _connectionString = connectionString;
        _keyPrefix = keyPrefix;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_connection != null) return;

        var options = ConfigurationOptions.Parse(_connectionString);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 3;
        options.ConnectTimeout = 5000;

        _connection = await ConnectionMultiplexer.ConnectAsync(options);
        _db = _connection.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);
        if (_db == null) return default;

        _lastAccess = DateTime.UtcNow;
        var fullKey = _keyPrefix + key;

        try
        {
            var value = await _db.StringGetAsync(fullKey);
            if (value.HasValue)
            {
                Interlocked.Increment(ref _hitCount);
                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
        }
        catch (RedisConnectionException)
        {
            // Connection lost, try to reconnect
            _connection?.Dispose();
            _connection = null;
            _db = null;
        }

        Interlocked.Increment(ref _missCount);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);
        if (_db == null) return;

        options ??= CacheEntryOptions.Default;
        var fullKey = _keyPrefix + key;
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var expiry = options.AbsoluteExpiration ?? options.SlidingExpiration;

        try
        {
            await _db.StringSetAsync(fullKey, json, expiry);
            _lastAccess = DateTime.UtcNow;

            if (options.Tags?.Length > 0)
            {
                foreach (var tag in options.Tags)
                {
                    await _db.SetAddAsync($"{_keyPrefix}tag:{tag}", key);
                }
            }
        }
        catch (RedisConnectionException)
        {
            _connection?.Dispose();
            _connection = null;
            _db = null;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);
        if (_db == null) return false;

        var fullKey = _keyPrefix + key;
        return await _db.KeyExistsAsync(fullKey);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);
        if (_db == null) return;

        var fullKey = _keyPrefix + key;
        await _db.KeyDeleteAsync(fullKey);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);
        if (_db == null || _connection == null) return;

        var endpoints = _connection.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _connection.GetServer(endpoint);
            var keys = server.Keys(pattern: $"{_keyPrefix}*").ToArray();
            if (keys.Length > 0)
            {
                await _db.KeyDeleteAsync(keys);
            }
        }
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);

        var result = new Dictionary<string, T?>();
        var keyList = keys.ToList();

        if (_db == null)
        {
            foreach (var key in keyList)
                result[key] = default;
            return result;
        }

        var redisKeys = keyList.Select(k => (RedisKey)(_keyPrefix + k)).ToArray();
        var values = await _db.StringGetAsync(redisKeys);

        for (int i = 0; i < keyList.Count; i++)
        {
            if (values[i].HasValue)
            {
                result[keyList[i]] = JsonSerializer.Deserialize<T>(values[i]!, _jsonOptions);
                Interlocked.Increment(ref _hitCount);
            }
            else
            {
                result[keyList[i]] = default;
                Interlocked.Increment(ref _missCount);
            }
        }

        _lastAccess = DateTime.UtcNow;
        return result;
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);
        if (_db == null) return;

        options ??= CacheEntryOptions.Default;
        var batch = _db.CreateBatch();
        var tasks = new List<Task>();

        foreach (var kvp in items)
        {
            var fullKey = _keyPrefix + kvp.Key;
            var json = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
            var expiry = options.AbsoluteExpiration ?? options.SlidingExpiration;
            tasks.Add(batch.StringSetAsync(fullKey, json, expiry));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
        _lastAccess = DateTime.UtcNow;
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        if (_db == null) await ConnectAsync(ct);
        if (_db == null) return;

        var tagKey = $"{_keyPrefix}tag:{tag}";
        var members = await _db.SetMembersAsync(tagKey);

        var keysToDelete = members
            .Select(m => (RedisKey)(_keyPrefix + m.ToString()))
            .Concat(new[] { (RedisKey)tagKey })
            .ToArray();

        if (keysToDelete.Length > 0)
        {
            await _db.KeyDeleteAsync(keysToDelete);
        }
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var hitRate = _hitCount + _missCount > 0
            ? (double)_hitCount / (_hitCount + _missCount)
            : 0;

        return Task.FromResult(new CacheStatistics(
            Layer,
            -1, // Redis doesn't easily expose total count without scanning
            -1, // Size estimation requires scanning
            _hitCount,
            _missCount,
            hitRate,
            _lastAccess
        ));
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
