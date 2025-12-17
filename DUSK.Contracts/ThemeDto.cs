namespace DUSK.Contracts;

using System.Text.Json.Serialization;

/// <summary>
/// Theme configuration DTO for API exchange.
/// </summary>
public class ThemeDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("style")]
    public string Style { get; set; } = "AmigaMUI";

    [JsonPropertyName("colors")]
    public ThemeColorsDto Colors { get; set; } = new();

    [JsonPropertyName("fonts")]
    public ThemeFontsDto Fonts { get; set; } = new();

    [JsonPropertyName("metrics")]
    public ThemeMetricsDto Metrics { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ThemeColorsDto
{
    [JsonPropertyName("windowBackground")]
    public ColorDto WindowBackground { get; set; } = new();

    [JsonPropertyName("controlBackground")]
    public ColorDto ControlBackground { get; set; } = new();

    [JsonPropertyName("controlForeground")]
    public ColorDto ControlForeground { get; set; } = new();

    [JsonPropertyName("accent")]
    public ColorDto Accent { get; set; } = new();

    [JsonPropertyName("accentDark")]
    public ColorDto AccentDark { get; set; } = new();

    [JsonPropertyName("borderLight")]
    public ColorDto BorderLight { get; set; } = new();

    [JsonPropertyName("borderDark")]
    public ColorDto BorderDark { get; set; } = new();

    [JsonPropertyName("text")]
    public ColorDto Text { get; set; } = new();

    [JsonPropertyName("textDisabled")]
    public ColorDto TextDisabled { get; set; } = new();

    [JsonPropertyName("selection")]
    public ColorDto Selection { get; set; } = new();

    [JsonPropertyName("error")]
    public ColorDto Error { get; set; } = new();

    [JsonPropertyName("warning")]
    public ColorDto Warning { get; set; } = new();

    [JsonPropertyName("success")]
    public ColorDto Success { get; set; } = new();
}

public class ColorDto
{
    [JsonPropertyName("r")]
    public byte R { get; set; }

    [JsonPropertyName("g")]
    public byte G { get; set; }

    [JsonPropertyName("b")]
    public byte B { get; set; }

    [JsonPropertyName("a")]
    public byte A { get; set; } = 255;

    [JsonPropertyName("hex")]
    public string? Hex { get; set; }

    public ColorDto() { }

    public ColorDto(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
        Hex = $"#{r:X2}{g:X2}{b:X2}";
    }

    public static ColorDto FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return new ColorDto
        {
            R = Convert.ToByte(hex[..2], 16),
            G = Convert.ToByte(hex[2..4], 16),
            B = Convert.ToByte(hex[4..6], 16),
            A = hex.Length >= 8 ? Convert.ToByte(hex[6..8], 16) : (byte)255,
            Hex = $"#{hex}"
        };
    }
}

public class ThemeFontsDto
{
    [JsonPropertyName("default")]
    public FontDto Default { get; set; } = new("Topaz", 8);

    [JsonPropertyName("title")]
    public FontDto Title { get; set; } = new("Topaz", 12);

    [JsonPropertyName("heading")]
    public FontDto Heading { get; set; } = new("Topaz", 10);

    [JsonPropertyName("body")]
    public FontDto Body { get; set; } = new("Topaz", 8);

    [JsonPropertyName("monospace")]
    public FontDto Monospace { get; set; } = new("Courier", 8);
}

public class FontDto
{
    [JsonPropertyName("family")]
    public string Family { get; set; } = "Default";

    [JsonPropertyName("size")]
    public int Size { get; set; } = 12;

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("italic")]
    public bool Italic { get; set; }

    public FontDto() { }
    public FontDto(string family, int size, bool bold = false, bool italic = false)
    {
        Family = family; Size = size; Bold = bold; Italic = italic;
    }
}

public class ThemeMetricsDto
{
    [JsonPropertyName("borderWidth")]
    public int BorderWidth { get; set; } = 1;

    [JsonPropertyName("borderRadius")]
    public int BorderRadius { get; set; } = 0;

    [JsonPropertyName("buttonPadding")]
    public int ButtonPadding { get; set; } = 8;

    [JsonPropertyName("inputPadding")]
    public int InputPadding { get; set; } = 4;

    [JsonPropertyName("elementSpacing")]
    public int ElementSpacing { get; set; } = 4;

    [JsonPropertyName("windowPadding")]
    public int WindowPadding { get; set; } = 8;

    [JsonPropertyName("bevelDepth")]
    public int BevelDepth { get; set; } = 2;

    [JsonPropertyName("scrollbarWidth")]
    public int ScrollbarWidth { get; set; } = 16;
}
