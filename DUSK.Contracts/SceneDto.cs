namespace DUSK.Contracts;

using System.Text.Json.Serialization;

/// <summary>
/// Scene configuration DTO for API exchange.
/// </summary>
public class SceneDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("type")]
    public string? SceneType { get; set; }

    [JsonPropertyName("bounds")]
    public RectDto Bounds { get; set; } = new();

    [JsonPropertyName("backgroundColor")]
    public ColorDto BackgroundColor { get; set; } = new();

    [JsonPropertyName("themeId")]
    public string? ThemeId { get; set; }

    [JsonPropertyName("elements")]
    public List<ElementDto> Elements { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// UI Element DTO for API exchange.
/// </summary>
public class ElementDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("bounds")]
    public RectDto Bounds { get; set; } = new();

    [JsonPropertyName("padding")]
    public InsetsDto Padding { get; set; } = new();

    [JsonPropertyName("margin")]
    public InsetsDto Margin { get; set; } = new();

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("children")]
    public List<ElementDto> Children { get; set; } = new();

    [JsonPropertyName("events")]
    public List<EventBindingDto>? Events { get; set; }
}

public class RectDto
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    public RectDto() { }
    public RectDto(int x, int y, int width, int height)
    {
        X = x; Y = y; Width = width; Height = height;
    }
}

public class InsetsDto
{
    [JsonPropertyName("left")]
    public int Left { get; set; }

    [JsonPropertyName("top")]
    public int Top { get; set; }

    [JsonPropertyName("right")]
    public int Right { get; set; }

    [JsonPropertyName("bottom")]
    public int Bottom { get; set; }

    public InsetsDto() { }
    public InsetsDto(int all) : this(all, all, all, all) { }
    public InsetsDto(int left, int top, int right, int bottom)
    {
        Left = left; Top = top; Right = right; Bottom = bottom;
    }
}

public class EventBindingDto
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = "";

    [JsonPropertyName("handler")]
    public string Handler { get; set; } = "";

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}
