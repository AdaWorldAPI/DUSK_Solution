namespace DUSK.Prediction;

/// <summary>
/// Core interface for prediction services.
/// Supports multiple ML backends (local, cloud, edge).
/// </summary>
public interface IPredictionService
{
    /// <summary>
    /// Service identifier.
    /// </summary>
    string ServiceId { get; }

    /// <summary>
    /// Whether the service is available and ready.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Make a prediction with the given input.
    /// </summary>
    Task<PredictionResult<TOutput>> PredictAsync<TInput, TOutput>(
        TInput input,
        PredictionOptions? options = null,
        CancellationToken ct = default
    ) where TInput : class where TOutput : class;

    /// <summary>
    /// Batch prediction for multiple inputs.
    /// </summary>
    Task<IReadOnlyList<PredictionResult<TOutput>>> PredictBatchAsync<TInput, TOutput>(
        IEnumerable<TInput> inputs,
        PredictionOptions? options = null,
        CancellationToken ct = default
    ) where TInput : class where TOutput : class;

    /// <summary>
    /// Get available models.
    /// </summary>
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// Load a specific model.
    /// </summary>
    Task<bool> LoadModelAsync(string modelId, CancellationToken ct = default);

    /// <summary>
    /// Unload a model to free resources.
    /// </summary>
    Task UnloadModelAsync(string modelId, CancellationToken ct = default);
}

/// <summary>
/// Result of a prediction operation.
/// </summary>
public class PredictionResult<T> where T : class
{
    public bool Success { get; init; }
    public T? Output { get; init; }
    public float Confidence { get; init; }
    public TimeSpan Latency { get; init; }
    public string? ModelId { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, float>? Probabilities { get; init; }

    public static PredictionResult<T> Ok(T output, float confidence, TimeSpan latency, string? modelId = null)
        => new() { Success = true, Output = output, Confidence = confidence, Latency = latency, ModelId = modelId };

    public static PredictionResult<T> Fail(string error, TimeSpan latency)
        => new() { Success = false, Error = error, Latency = latency };
}

/// <summary>
/// Options for prediction requests.
/// </summary>
public record PredictionOptions
{
    public string? ModelId { get; init; }
    public float MinConfidence { get; init; } = 0.5f;
    public int MaxResults { get; init; } = 1;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public bool UseCache { get; init; } = true;
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Information about an available model.
/// </summary>
public record ModelInfo
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public ModelType Type { get; init; }
    public string Version { get; init; } = "";
    public bool IsLoaded { get; init; }
    public long SizeBytes { get; init; }
    public IReadOnlyList<string> InputTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OutputTypes { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Types of ML models.
/// </summary>
public enum ModelType
{
    Classification,
    Regression,
    Clustering,
    ObjectDetection,
    TextGeneration,
    ImageGeneration,
    Embedding,
    Custom
}
