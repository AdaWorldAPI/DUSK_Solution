namespace DUSK.Prediction;

using System.Diagnostics;
using DUSK.Sync;

/// <summary>
/// Orchestrates predictions across multiple services with caching and fallback.
/// Similar philosophy to CacheOrchestrator - tries fast services first, falls back to slower.
/// </summary>
public sealed class PredictionOrchestrator : IDisposable
{
    private readonly List<IPredictionService> _services = new();
    private readonly ICacheOrchestrator? _cache;
    private bool _disposed;

    public PredictionOrchestrator(ICacheOrchestrator? cache = null)
    {
        _cache = cache;
    }

    /// <summary>
    /// Register a prediction service (order matters - first is preferred).
    /// </summary>
    public void RegisterService(IPredictionService service)
    {
        _services.Add(service);
    }

    /// <summary>
    /// Make a prediction, trying services in order until one succeeds.
    /// </summary>
    public async Task<PredictionResult<TOutput>> PredictAsync<TInput, TOutput>(
        TInput input,
        PredictionOptions? options = null,
        CancellationToken ct = default
    ) where TInput : class where TOutput : class
    {
        options ??= new PredictionOptions();

        // Try cache first
        if (options.UseCache && _cache != null)
        {
            var cacheKey = GenerateCacheKey<TInput, TOutput>(input, options);
            var cached = await _cache.GetAsync<PredictionResult<TOutput>>(cacheKey, ct);
            if (cached != null)
            {
                return cached with { Latency = TimeSpan.Zero };
            }
        }

        var sw = Stopwatch.StartNew();
        PredictionResult<TOutput>? lastResult = null;

        foreach (var service in _services.Where(s => s.IsAvailable))
        {
            try
            {
                var result = await service.PredictAsync<TInput, TOutput>(input, options, ct);
                if (result.Success && result.Confidence >= options.MinConfidence)
                {
                    sw.Stop();

                    // Cache successful result
                    if (options.UseCache && _cache != null)
                    {
                        var cacheKey = GenerateCacheKey<TInput, TOutput>(input, options);
                        await _cache.SetAsync(cacheKey, result, ct: ct);
                    }

                    return result with { Latency = sw.Elapsed };
                }
                lastResult = result;
            }
            catch (Exception ex)
            {
                lastResult = PredictionResult<TOutput>.Fail($"Service {service.ServiceId} error: {ex.Message}", sw.Elapsed);
            }
        }

        sw.Stop();
        return lastResult ?? PredictionResult<TOutput>.Fail("No prediction services available", sw.Elapsed);
    }

    /// <summary>
    /// Get aggregated model list from all services.
    /// </summary>
    public async Task<IReadOnlyList<ModelInfo>> GetAllModelsAsync(CancellationToken ct = default)
    {
        var models = new List<ModelInfo>();
        foreach (var service in _services.Where(s => s.IsAvailable))
        {
            try
            {
                var serviceModels = await service.GetAvailableModelsAsync(ct);
                models.AddRange(serviceModels);
            }
            catch
            {
                // Skip unavailable services
            }
        }
        return models;
    }

    private static string GenerateCacheKey<TInput, TOutput>(TInput input, PredictionOptions options)
    {
        var inputHash = input.GetHashCode();
        var modelPart = options.ModelId ?? "default";
        return $"prediction:{typeof(TInput).Name}:{typeof(TOutput).Name}:{modelPart}:{inputHash}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var service in _services.OfType<IDisposable>())
        {
            service.Dispose();
        }
        _services.Clear();
        _disposed = true;
    }
}
