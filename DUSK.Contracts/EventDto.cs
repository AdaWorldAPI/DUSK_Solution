namespace DUSK.Contracts;

using System.Text.Json.Serialization;

/// <summary>
/// Event DTO for real-time notifications.
/// </summary>
public class EventDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// UI event notification.
/// </summary>
public class UIEventDto : EventDto
{
    [JsonPropertyName("sceneId")]
    public string SceneId { get; set; } = "";

    [JsonPropertyName("elementId")]
    public string? ElementId { get; set; }

    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = "";

    [JsonPropertyName("position")]
    public PointDto? Position { get; set; }
}

/// <summary>
/// Cache pulse event for waveform visualization.
/// </summary>
public class PulseEventDto : EventDto
{
    [JsonPropertyName("layer")]
    public string Layer { get; set; } = "";

    [JsonPropertyName("pulseType")]
    public string PulseType { get; set; } = "";

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("intensity")]
    public float Intensity { get; set; }

    [JsonPropertyName("latencyMs")]
    public double LatencyMs { get; set; }

    [JsonPropertyName("waveformSamples")]
    public float[]? WaveformSamples { get; set; }
}

/// <summary>
/// Theme change event.
/// </summary>
public class ThemeChangeEventDto : EventDto
{
    [JsonPropertyName("previousThemeId")]
    public string? PreviousThemeId { get; set; }

    [JsonPropertyName("newThemeId")]
    public string NewThemeId { get; set; } = "";

    [JsonPropertyName("mood")]
    public string? Mood { get; set; }
}

/// <summary>
/// Scene navigation event.
/// </summary>
public class NavigationEventDto : EventDto
{
    [JsonPropertyName("fromSceneId")]
    public string? FromSceneId { get; set; }

    [JsonPropertyName("toSceneId")]
    public string ToSceneId { get; set; } = "";

    [JsonPropertyName("transitionType")]
    public string? TransitionType { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "forward"; // forward, back, replace
}

/// <summary>
/// Prediction event.
/// </summary>
public class PredictionEventDto : EventDto
{
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = "";

    [JsonPropertyName("inputType")]
    public string InputType { get; set; } = "";

    [JsonPropertyName("outputType")]
    public string OutputType { get; set; } = "";

    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }

    [JsonPropertyName("latencyMs")]
    public double LatencyMs { get; set; }

    [JsonPropertyName("cached")]
    public bool Cached { get; set; }
}

/// <summary>
/// Error event.
/// </summary>
public class ErrorEventDto : EventDto
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Error"; // Info, Warning, Error, Critical
}

public class PointDto
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    public PointDto() { }
    public PointDto(int x, int y) { X = x; Y = y; }
}
