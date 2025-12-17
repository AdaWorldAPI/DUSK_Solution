namespace DUSK.Core;

/// <summary>
/// Core rendering abstraction for DUSK framework.
/// Implementations provide platform-specific rendering (Console, WPF, WinForms, Unity).
/// </summary>
public interface IRenderer : IDisposable
{
    string Name { get; }
    bool IsInitialized { get; }
    RenderCapabilities Capabilities { get; }

    void Initialize(RenderConfiguration config);
    void BeginFrame();
    void EndFrame();
    void Clear(DuskColor color);

    void DrawRectangle(DuskRect rect, DuskColor color, bool filled = true);
    void DrawRectangleBeveled(DuskRect rect, DuskColor color, BevelStyle bevel, int depth = 2);
    void DrawText(string text, DuskPoint position, DuskFont font, DuskColor color);
    void DrawLine(DuskPoint start, DuskPoint end, DuskColor color, int thickness = 1);
    void DrawImage(DuskImage image, DuskRect destination);

    void SetClipRegion(DuskRect? region);
    DuskSize MeasureText(string text, DuskFont font);

    void Present();
}

public record RenderConfiguration(
    int Width,
    int Height,
    bool VSync = true,
    bool DoubleBuffered = true
);

public record RenderCapabilities(
    bool SupportsTransparency,
    bool SupportsGradients,
    bool SupportsBeveledEdges,
    bool SupportsAnimations,
    int MaxTextureSize
);

public enum BevelStyle
{
    None,
    Raised,
    Sunken,
    Etched,
    RaisedSoft,
    SunkenSoft,
    AmigaMUI
}

public record struct DuskColor(byte R, byte G, byte B, byte A = 255)
{
    public static DuskColor Transparent => new(0, 0, 0, 0);
    public static DuskColor Black => new(0, 0, 0);
    public static DuskColor White => new(255, 255, 255);
    public static DuskColor AmigaGray => new(170, 170, 170);
    public static DuskColor AmigaBlue => new(0, 85, 170);
    public static DuskColor AmigaOrange => new(255, 136, 0);

    public DuskColor WithAlpha(byte alpha) => new(R, G, B, alpha);
    public DuskColor Lerp(DuskColor other, float t) => new(
        (byte)(R + (other.R - R) * t),
        (byte)(G + (other.G - G) * t),
        (byte)(B + (other.B - B) * t),
        (byte)(A + (other.A - A) * t)
    );
}

public record struct DuskPoint(int X, int Y)
{
    public static DuskPoint Zero => new(0, 0);
    public DuskPoint Offset(int dx, int dy) => new(X + dx, Y + dy);
}

public record struct DuskSize(int Width, int Height)
{
    public static DuskSize Zero => new(0, 0);
}

public record struct DuskRect(int X, int Y, int Width, int Height)
{
    public static DuskRect Empty => new(0, 0, 0, 0);
    public DuskPoint Location => new(X, Y);
    public DuskSize Size => new(Width, Height);
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool Contains(DuskPoint point) =>
        point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    public DuskRect Inflate(int dx, int dy) => new(X - dx, Y - dy, Width + dx * 2, Height + dy * 2);
    public DuskRect Offset(int dx, int dy) => new(X + dx, Y + dy, Width, Height);
}

public record DuskFont(string Family, int Size, DuskFontStyle Style = DuskFontStyle.Regular)
{
    public static DuskFont Default => new("Topaz", 8);
    public static DuskFont DefaultBold => new("Topaz", 8, DuskFontStyle.Bold);
}

[Flags]
public enum DuskFontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4
}

public record DuskImage(int Width, int Height, byte[] PixelData);
