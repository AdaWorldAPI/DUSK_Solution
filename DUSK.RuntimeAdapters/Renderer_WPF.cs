namespace DUSK.RuntimeAdapters;

using DUSK.Core;
using System.Runtime.Versioning;

/// <summary>
/// WPF-based renderer for Windows desktop applications.
/// Uses DrawingVisual for high-performance rendering.
/// </summary>
[SupportedOSPlatform("windows")]
public class Renderer_WPF : IRenderer
{
    private RenderConfiguration _config = new(800, 600);
    private System.Windows.Media.DrawingGroup? _drawingGroup;
    private System.Windows.Media.DrawingContext? _currentContext;
    private DuskRect? _clipRegion;
    private bool _initialized;

    public string Name => "WPF";
    public bool IsInitialized => _initialized;

    public RenderCapabilities Capabilities => new(
        SupportsTransparency: true,
        SupportsGradients: true,
        SupportsBeveledEdges: true,
        SupportsAnimations: true,
        MaxTextureSize: 16384
    );

    public void Initialize(RenderConfiguration config)
    {
        _config = config;
        _drawingGroup = new System.Windows.Media.DrawingGroup();
        _initialized = true;
    }

    public void BeginFrame()
    {
        _drawingGroup = new System.Windows.Media.DrawingGroup();
        _currentContext = _drawingGroup.Open();
    }

    public void EndFrame()
    {
        _currentContext?.Close();
        _currentContext = null;
    }

    public void Clear(DuskColor color)
    {
        var brush = new System.Windows.Media.SolidColorBrush(ToWpfColor(color));
        brush.Freeze();
        _currentContext?.DrawRectangle(brush, null,
            new System.Windows.Rect(0, 0, _config.Width, _config.Height));
    }

    public void DrawRectangle(DuskRect rect, DuskColor color, bool filled = true)
    {
        if (_currentContext == null) return;

        var wpfRect = new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height);
        var brush = filled ? new System.Windows.Media.SolidColorBrush(ToWpfColor(color)) : null;
        brush?.Freeze();

        var pen = !filled ? new System.Windows.Media.Pen(
            new System.Windows.Media.SolidColorBrush(ToWpfColor(color)), 1) : null;
        pen?.Freeze();

        _currentContext.DrawRectangle(brush, pen, wpfRect);
    }

    public void DrawRectangleBeveled(DuskRect rect, DuskColor color, BevelStyle bevel, int depth = 2)
    {
        if (_currentContext == null) return;

        // Draw base rectangle
        DrawRectangle(rect, color, true);

        if (bevel == BevelStyle.None) return;

        var isRaised = bevel is BevelStyle.Raised or BevelStyle.RaisedSoft or BevelStyle.AmigaMUI;
        var lightColor = isRaised ? Lighten(color, 0.3f) : Darken(color, 0.3f);
        var darkColor = isRaised ? Darken(color, 0.3f) : Lighten(color, 0.3f);

        var lightBrush = new System.Windows.Media.SolidColorBrush(ToWpfColor(lightColor));
        var darkBrush = new System.Windows.Media.SolidColorBrush(ToWpfColor(darkColor));
        lightBrush.Freeze();
        darkBrush.Freeze();

        var lightPen = new System.Windows.Media.Pen(lightBrush, depth);
        var darkPen = new System.Windows.Media.Pen(darkBrush, depth);
        lightPen.Freeze();
        darkPen.Freeze();

        // Top edge
        _currentContext.DrawLine(lightPen,
            new System.Windows.Point(rect.X, rect.Y),
            new System.Windows.Point(rect.Right, rect.Y));

        // Left edge
        _currentContext.DrawLine(lightPen,
            new System.Windows.Point(rect.X, rect.Y),
            new System.Windows.Point(rect.X, rect.Bottom));

        // Bottom edge
        _currentContext.DrawLine(darkPen,
            new System.Windows.Point(rect.X, rect.Bottom),
            new System.Windows.Point(rect.Right, rect.Bottom));

        // Right edge
        _currentContext.DrawLine(darkPen,
            new System.Windows.Point(rect.Right, rect.Y),
            new System.Windows.Point(rect.Right, rect.Bottom));
    }

    public void DrawText(string text, DuskPoint position, DuskFont font, DuskColor color)
    {
        if (_currentContext == null || string.IsNullOrEmpty(text)) return;

        var typeface = new System.Windows.Media.Typeface(
            new System.Windows.Media.FontFamily(font.Family),
            font.Style.HasFlag(DuskFontStyle.Italic)
                ? System.Windows.FontStyles.Italic
                : System.Windows.FontStyles.Normal,
            font.Style.HasFlag(DuskFontStyle.Bold)
                ? System.Windows.FontWeights.Bold
                : System.Windows.FontWeights.Normal,
            System.Windows.FontStretches.Normal
        );

        var formattedText = new System.Windows.Media.FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            font.Size,
            new System.Windows.Media.SolidColorBrush(ToWpfColor(color)),
            96 // pixelsPerDip
        );

        _currentContext.DrawText(formattedText, new System.Windows.Point(position.X, position.Y));
    }

    public void DrawLine(DuskPoint start, DuskPoint end, DuskColor color, int thickness = 1)
    {
        if (_currentContext == null) return;

        var brush = new System.Windows.Media.SolidColorBrush(ToWpfColor(color));
        brush.Freeze();
        var pen = new System.Windows.Media.Pen(brush, thickness);
        pen.Freeze();

        _currentContext.DrawLine(pen,
            new System.Windows.Point(start.X, start.Y),
            new System.Windows.Point(end.X, end.Y));
    }

    public void DrawImage(DuskImage image, DuskRect destination)
    {
        if (_currentContext == null) return;

        // Convert DUSK image to WPF BitmapSource
        var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(
            image.Width, image.Height,
            96, 96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            image.PixelData,
            image.Width * 4
        );

        _currentContext.DrawImage(bitmap,
            new System.Windows.Rect(destination.X, destination.Y, destination.Width, destination.Height));
    }

    public void SetClipRegion(DuskRect? region)
    {
        _clipRegion = region;

        if (_currentContext == null) return;

        if (region.HasValue)
        {
            var geometry = new System.Windows.Media.RectangleGeometry(
                new System.Windows.Rect(region.Value.X, region.Value.Y,
                                       region.Value.Width, region.Value.Height));
            _currentContext.PushClip(geometry);
        }
        else
        {
            _currentContext.Pop();
        }
    }

    public DuskSize MeasureText(string text, DuskFont font)
    {
        if (string.IsNullOrEmpty(text)) return DuskSize.Zero;

        var typeface = new System.Windows.Media.Typeface(
            new System.Windows.Media.FontFamily(font.Family),
            System.Windows.FontStyles.Normal,
            System.Windows.FontWeights.Normal,
            System.Windows.FontStretches.Normal
        );

        var formattedText = new System.Windows.Media.FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            font.Size,
            System.Windows.Media.Brushes.Black,
            96
        );

        return new DuskSize((int)formattedText.Width, (int)formattedText.Height);
    }

    public void Present()
    {
        // In actual WPF app, this would update the visual tree
        // For standalone use, we just finalize the drawing group
    }

    public System.Windows.Media.DrawingGroup? GetDrawingGroup() => _drawingGroup;

    private static System.Windows.Media.Color ToWpfColor(DuskColor color)
    {
        return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    private static DuskColor Lighten(DuskColor color, float amount)
    {
        return new DuskColor(
            (byte)Math.Min(255, color.R + (255 - color.R) * amount),
            (byte)Math.Min(255, color.G + (255 - color.G) * amount),
            (byte)Math.Min(255, color.B + (255 - color.B) * amount),
            color.A
        );
    }

    private static DuskColor Darken(DuskColor color, float amount)
    {
        return new DuskColor(
            (byte)(color.R * (1 - amount)),
            (byte)(color.G * (1 - amount)),
            (byte)(color.B * (1 - amount)),
            color.A
        );
    }

    public void Dispose()
    {
        _currentContext?.Close();
        _currentContext = null;
        _drawingGroup = null;
    }
}
