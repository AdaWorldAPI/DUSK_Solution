namespace DUSK.Sync.Providers;

using System.Text.Json;
using DUSK.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

/// <summary>
/// L3 Cache: MongoDB persistent cache for long-term storage.
/// Typical latency: 10-50ms
/// </summary>
public sealed class L3_MongoCache : ICacheProvider, IDisposable
{
    private MongoClient? _client;
    private IMongoDatabase? _database;
    private IMongoCollection<CacheDocument>? _collection;
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly string _collectionName;
    private readonly JsonSerializerOptions _jsonOptions;
    private long _hitCount;
    private long _missCount;
    private DateTime _lastAccess = DateTime.UtcNow;

    public string Name => "L3_MongoDB";
    public CacheLayer Layer => CacheLayer.L3_MongoDB;
    public bool IsConnected => _client != null;

    public L3_MongoCache(
        string connectionString = "mongodb://localhost:27017",
        string databaseName = "dusk_cache",
        string collectionName = "cache")
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
        _collectionName = collectionName;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_client != null) return;

        var settings = MongoClientSettings.FromConnectionString(_connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.ConnectTimeout = TimeSpan.FromSeconds(5);

        _client = new MongoClient(settings);
        _database = _client.GetDatabase(_databaseName);
        _collection = _database.GetCollection<CacheDocument>(_collectionName);

        // Create indexes
        var indexKeys = Builders<CacheDocument>.IndexKeys;
        var indexes = new[]
        {
            new CreateIndexModel<CacheDocument>(indexKeys.Ascending(x => x.Key), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<CacheDocument>(indexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }),
            new CreateIndexModel<CacheDocument>(indexKeys.Ascending(x => x.Tags))
        };

        await _collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);
        if (_collection == null) return default;

        _lastAccess = DateTime.UtcNow;

        try
        {
            var filter = Builders<CacheDocument>.Filter.Eq(x => x.Key, key);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);

            if (doc != null && (doc.ExpiresAt == null || doc.ExpiresAt > DateTime.UtcNow))
            {
                Interlocked.Increment(ref _hitCount);

                // Update last accessed time for sliding expiration
                if (doc.SlidingExpiration.HasValue)
                {
                    var update = Builders<CacheDocument>.Update
                        .Set(x => x.LastAccess, DateTime.UtcNow)
                        .Set(x => x.ExpiresAt, DateTime.UtcNow.Add(doc.SlidingExpiration.Value));
                    await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
                }

                return JsonSerializer.Deserialize<T>(doc.Value, _jsonOptions);
            }
        }
        catch (MongoException)
        {
            // Connection issues
        }

        Interlocked.Increment(ref _missCount);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);
        if (_collection == null) return;

        options ??= CacheEntryOptions.LongLived; // L3 defaults to longer TTL
        var json = JsonSerializer.Serialize(value, _jsonOptions);

        DateTime? expiresAt = null;
        if (options.AbsoluteExpiration.HasValue)
            expiresAt = DateTime.UtcNow.Add(options.AbsoluteExpiration.Value);
        else if (options.SlidingExpiration.HasValue)
            expiresAt = DateTime.UtcNow.Add(options.SlidingExpiration.Value);

        var doc = new CacheDocument
        {
            Key = key,
            Value = json,
            CreatedAt = DateTime.UtcNow,
            LastAccess = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            SlidingExpiration = options.SlidingExpiration,
            Priority = options.Priority.ToString(),
            Tags = options.Tags?.ToList()
        };

        var filter = Builders<CacheDocument>.Filter.Eq(x => x.Key, key);
        await _collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
        _lastAccess = DateTime.UtcNow;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);
        if (_collection == null) return false;

        var filter = Builders<CacheDocument>.Filter.And(
            Builders<CacheDocument>.Filter.Eq(x => x.Key, key),
            Builders<CacheDocument>.Filter.Or(
                Builders<CacheDocument>.Filter.Eq(x => x.ExpiresAt, null),
                Builders<CacheDocument>.Filter.Gt(x => x.ExpiresAt, DateTime.UtcNow)
            )
        );

        return await _collection.CountDocumentsAsync(filter, cancellationToken: ct) > 0;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);
        if (_collection == null) return;

        var filter = Builders<CacheDocument>.Filter.Eq(x => x.Key, key);
        await _collection.DeleteOneAsync(filter, ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);
        if (_collection == null) return;

        await _collection.DeleteManyAsync(FilterDefinition<CacheDocument>.Empty, ct);
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);

        var result = new Dictionary<string, T?>();
        var keyList = keys.ToList();

        if (_collection == null)
        {
            foreach (var key in keyList)
                result[key] = default;
            return result;
        }

        var filter = Builders<CacheDocument>.Filter.In(x => x.Key, keyList);
        var docs = await _collection.Find(filter).ToListAsync(ct);
        var docDict = docs.ToDictionary(d => d.Key);

        foreach (var key in keyList)
        {
            if (docDict.TryGetValue(key, out var doc) &&
                (doc.ExpiresAt == null || doc.ExpiresAt > DateTime.UtcNow))
            {
                result[key] = JsonSerializer.Deserialize<T>(doc.Value, _jsonOptions);
                Interlocked.Increment(ref _hitCount);
            }
            else
            {
                result[key] = default;
                Interlocked.Increment(ref _missCount);
            }
        }

        _lastAccess = DateTime.UtcNow;
        return result;
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);
        if (_collection == null) return;

        options ??= CacheEntryOptions.LongLived;

        DateTime? expiresAt = null;
        if (options.AbsoluteExpiration.HasValue)
            expiresAt = DateTime.UtcNow.Add(options.AbsoluteExpiration.Value);
        else if (options.SlidingExpiration.HasValue)
            expiresAt = DateTime.UtcNow.Add(options.SlidingExpiration.Value);

        var operations = items.Select(kvp =>
        {
            var json = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
            var doc = new CacheDocument
            {
                Key = kvp.Key,
                Value = json,
                CreatedAt = DateTime.UtcNow,
                LastAccess = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                SlidingExpiration = options.SlidingExpiration,
                Priority = options.Priority.ToString(),
                Tags = options.Tags?.ToList()
            };

            return new ReplaceOneModel<CacheDocument>(
                Builders<CacheDocument>.Filter.Eq(x => x.Key, kvp.Key),
                doc
            ) { IsUpsert = true };
        }).ToList();

        if (operations.Count > 0)
        {
            await _collection.BulkWriteAsync(operations, cancellationToken: ct);
        }

        _lastAccess = DateTime.UtcNow;
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);
        if (_collection == null) return;

        var filter = Builders<CacheDocument>.Filter.AnyEq(x => x.Tags, tag);
        await _collection.DeleteManyAsync(filter, ct);
    }

    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        if (_collection == null) await ConnectAsync(ct);

        long totalItems = 0;
        long totalSize = 0;

        if (_collection != null)
        {
            totalItems = await _collection.CountDocumentsAsync(FilterDefinition<CacheDocument>.Empty, cancellationToken: ct);

            var stats = await _database!.RunCommandAsync<BsonDocument>(new BsonDocument("collStats", _collectionName), cancellationToken: ct);
            if (stats.Contains("size"))
            {
                totalSize = stats["size"].ToInt64();
            }
        }

        var hitRate = _hitCount + _missCount > 0
            ? (double)_hitCount / (_hitCount + _missCount)
            : 0;

        return new CacheStatistics(
            Layer,
            totalItems,
            totalSize,
            _hitCount,
            _missCount,
            hitRate,
            _lastAccess
        );
    }

    public void Dispose()
    {
        // MongoClient doesn't need explicit disposal
        _client = null;
        _database = null;
        _collection = null;
    }

    [BsonIgnoreExtraElements]
    private class CacheDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("key")]
        public string Key { get; set; } = string.Empty;

        [BsonElement("value")]
        public string Value { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("lastAccess")]
        public DateTime LastAccess { get; set; }

        [BsonElement("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [BsonElement("slidingExpiration")]
        public TimeSpan? SlidingExpiration { get; set; }

        [BsonElement("priority")]
        public string Priority { get; set; } = "Normal";

        [BsonElement("tags")]
        public List<string>? Tags { get; set; }
    }
}
