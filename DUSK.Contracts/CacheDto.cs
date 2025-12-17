namespace DUSK.Contracts;

using System.Text.Json.Serialization;

/// <summary>
/// Cache statistics DTO for monitoring APIs.
/// </summary>
public class CacheStatsDto
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("l1")]
    public CacheLayerStatsDto L1 { get; set; } = new();

    [JsonPropertyName("l2")]
    public CacheLayerStatsDto L2 { get; set; } = new();

    [JsonPropertyName("l3")]
    public CacheLayerStatsDto L3 { get; set; } = new();

    [JsonPropertyName("overall")]
    public CacheOverallStatsDto Overall { get; set; } = new();
}

public class CacheLayerStatsDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("isConnected")]
    public bool IsConnected { get; set; }

    [JsonPropertyName("latencyMs")]
    public double LatencyMs { get; set; }

    [JsonPropertyName("itemCount")]
    public long ItemCount { get; set; }

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("hitCount")]
    public long HitCount { get; set; }

    [JsonPropertyName("missCount")]
    public long MissCount { get; set; }

    [JsonPropertyName("hitRate")]
    public double HitRate { get; set; }

    [JsonPropertyName("evictionCount")]
    public long EvictionCount { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class CacheOverallStatsDto
{
    [JsonPropertyName("totalHits")]
    public long TotalHits { get; set; }

    [JsonPropertyName("totalMisses")]
    public long TotalMisses { get; set; }

    [JsonPropertyName("totalWrites")]
    public long TotalWrites { get; set; }

    [JsonPropertyName("overallHitRate")]
    public double OverallHitRate { get; set; }

    [JsonPropertyName("averageLatencyMs")]
    public double AverageLatencyMs { get; set; }

    [JsonPropertyName("eventsPerSecond")]
    public double EventsPerSecond { get; set; }
}

/// <summary>
/// Cache entry DTO for API operations.
/// </summary>
public class CacheEntryDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("valueType")]
    public string? ValueType { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("slidingExpiration")]
    public TimeSpan? SlidingExpiration { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("layer")]
    public string Layer { get; set; } = "";
}

/// <summary>
/// Cache operation request DTO.
/// </summary>
public class CacheOperationDto
{
    [JsonPropertyName("operation")]
    public CacheOperation Operation { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("options")]
    public CacheOptionsDto? Options { get; set; }
}

public enum CacheOperation
{
    Get,
    Set,
    Delete,
    Exists,
    Clear,
    InvalidateByTag
}

public class CacheOptionsDto
{
    [JsonPropertyName("absoluteExpirationSeconds")]
    public int? AbsoluteExpirationSeconds { get; set; }

    [JsonPropertyName("slidingExpirationSeconds")]
    public int? SlidingExpirationSeconds { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "Normal";
}
