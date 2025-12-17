namespace DUSK.Contracts;

using System.Text.Json.Serialization;

/// <summary>
/// API specification for DUSK services.
/// Describes available endpoints and operations.
/// </summary>
public class ApiSpec
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "DUSK Framework API";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "Amiga MUI-style UI framework with 3-layer cache";

    [JsonPropertyName("endpoints")]
    public List<EndpointSpec> Endpoints { get; set; } = new();

    [JsonPropertyName("models")]
    public Dictionary<string, ModelSpec> Models { get; set; } = new();

    public static ApiSpec CreateDefault()
    {
        return new ApiSpec
        {
            Endpoints = new List<EndpointSpec>
            {
                // Scene endpoints
                new() { Path = "/api/scenes", Method = "GET", Description = "List all scenes", ResponseType = "SceneDto[]" },
                new() { Path = "/api/scenes/{id}", Method = "GET", Description = "Get scene by ID", ResponseType = "SceneDto" },
                new() { Path = "/api/scenes", Method = "POST", Description = "Create scene", RequestType = "SceneDto", ResponseType = "SceneDto" },
                new() { Path = "/api/scenes/{id}", Method = "PUT", Description = "Update scene", RequestType = "SceneDto", ResponseType = "SceneDto" },
                new() { Path = "/api/scenes/{id}", Method = "DELETE", Description = "Delete scene" },

                // Theme endpoints
                new() { Path = "/api/themes", Method = "GET", Description = "List all themes", ResponseType = "ThemeDto[]" },
                new() { Path = "/api/themes/{id}", Method = "GET", Description = "Get theme by ID", ResponseType = "ThemeDto" },
                new() { Path = "/api/themes", Method = "POST", Description = "Create theme", RequestType = "ThemeDto", ResponseType = "ThemeDto" },
                new() { Path = "/api/themes/current", Method = "GET", Description = "Get current theme", ResponseType = "ThemeDto" },
                new() { Path = "/api/themes/current", Method = "PUT", Description = "Set current theme", RequestType = "string" },

                // Cache endpoints
                new() { Path = "/api/cache/stats", Method = "GET", Description = "Get cache statistics", ResponseType = "CacheStatsDto" },
                new() { Path = "/api/cache/{key}", Method = "GET", Description = "Get cached value", ResponseType = "CacheEntryDto" },
                new() { Path = "/api/cache", Method = "POST", Description = "Set cached value", RequestType = "CacheOperationDto" },
                new() { Path = "/api/cache/{key}", Method = "DELETE", Description = "Delete cached value" },
                new() { Path = "/api/cache/invalidate", Method = "POST", Description = "Invalidate by tag", RequestType = "string" },

                // Prediction endpoints
                new() { Path = "/api/prediction/models", Method = "GET", Description = "List available models", ResponseType = "ModelInfoDto[]" },
                new() { Path = "/api/prediction/predict", Method = "POST", Description = "Make prediction", RequestType = "PredictionRequestDto", ResponseType = "PredictionResultDto" },

                // Events endpoints (WebSocket)
                new() { Path = "/ws/events", Method = "WS", Description = "Real-time event stream", ResponseType = "EventDto" },
                new() { Path = "/ws/pulse", Method = "WS", Description = "Cache pulse visualization data", ResponseType = "PulseEventDto" },
            },
            Models = new Dictionary<string, ModelSpec>
            {
                ["SceneDto"] = new() { Description = "Scene configuration", Properties = GetSceneDtoProperties() },
                ["ThemeDto"] = new() { Description = "Theme configuration", Properties = GetThemeDtoProperties() },
                ["CacheStatsDto"] = new() { Description = "Cache statistics", Properties = GetCacheStatsDtoProperties() },
            }
        };
    }

    private static Dictionary<string, PropertySpec> GetSceneDtoProperties() => new()
    {
        ["id"] = new() { Type = "string", Description = "Unique scene identifier" },
        ["title"] = new() { Type = "string", Description = "Scene title" },
        ["bounds"] = new() { Type = "RectDto", Description = "Scene bounds" },
        ["elements"] = new() { Type = "ElementDto[]", Description = "UI elements" },
    };

    private static Dictionary<string, PropertySpec> GetThemeDtoProperties() => new()
    {
        ["id"] = new() { Type = "string", Description = "Theme identifier" },
        ["name"] = new() { Type = "string", Description = "Theme name" },
        ["colors"] = new() { Type = "ThemeColorsDto", Description = "Color palette" },
        ["fonts"] = new() { Type = "ThemeFontsDto", Description = "Font definitions" },
    };

    private static Dictionary<string, PropertySpec> GetCacheStatsDtoProperties() => new()
    {
        ["l1"] = new() { Type = "CacheLayerStatsDto", Description = "L1 Memory cache stats" },
        ["l2"] = new() { Type = "CacheLayerStatsDto", Description = "L2 Redis cache stats" },
        ["l3"] = new() { Type = "CacheLayerStatsDto", Description = "L3 MongoDB cache stats" },
    };
}

public class EndpointSpec
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("method")]
    public string Method { get; set; } = "GET";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("requestType")]
    public string? RequestType { get; set; }

    [JsonPropertyName("responseType")]
    public string? ResponseType { get; set; }

    [JsonPropertyName("parameters")]
    public List<ParameterSpec>? Parameters { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class ParameterSpec
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("in")]
    public string In { get; set; } = "path"; // path, query, header, body

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class ModelSpec
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("properties")]
    public Dictionary<string, PropertySpec> Properties { get; set; } = new();
}

public class PropertySpec
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }
}
