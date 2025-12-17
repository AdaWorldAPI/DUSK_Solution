namespace DUSK.RuntimeAdapters;

using DUSK.Core;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

/// <summary>
/// Windows Forms GDI+ based renderer.
/// Critical for drop-in WinForms replacement functionality.
/// </summary>
[SupportedOSPlatform("windows")]
public class Renderer_WinForms : IRenderer
{
    private RenderConfiguration _config = new(800, 600);
    private Bitmap? _backBuffer;
    private Graphics? _graphics;
    private DuskRect? _clipRegion;
    private bool _initialized;

    // Font cache
    private readonly Dictionary<DuskFont, Font> _fontCache = new();

    public string Name => "WinForms";
    public bool IsInitialized => _initialized;

    public RenderCapabilities Capabilities => new(
        SupportsTransparency: true,
        SupportsGradients: true,
        SupportsBeveledEdges: true,
        SupportsAnimations: true,
        MaxTextureSize: 8192
    );

    public void Initialize(RenderConfiguration config)
    {
        _config = config;
        _backBuffer = new Bitmap(config.Width, config.Height);
        _graphics = Graphics.FromImage(_backBuffer);
        _graphics.SmoothingMode = SmoothingMode.AntiAlias;
        _graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        _initialized = true;
    }

    public void BeginFrame()
    {
        _graphics?.Clear(Color.Transparent);
    }

    public void EndFrame()
    {
        // Frame complete
    }

    public void Clear(DuskColor color)
    {
        _graphics?.Clear(ToGdiColor(color));
    }

    public void DrawRectangle(DuskRect rect, DuskColor color, bool filled = true)
    {
        if (_graphics == null) return;

        var gdiRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);

        if (filled)
        {
            using var brush = new SolidBrush(ToGdiColor(color));
            _graphics.FillRectangle(brush, gdiRect);
        }
        else
        {
            using var pen = new Pen(ToGdiColor(color));
            _graphics.DrawRectangle(pen, gdiRect);
        }
    }

    public void DrawRectangleBeveled(DuskRect rect, DuskColor color, BevelStyle bevel, int depth = 2)
    {
        if (_graphics == null) return;

        // Draw base rectangle
        DrawRectangle(rect, color, true);

        if (bevel == BevelStyle.None) return;

        var isRaised = bevel is BevelStyle.Raised or BevelStyle.RaisedSoft or BevelStyle.AmigaMUI;
        var lightColor = isRaised ? ControlPaint.Light(ToGdiColor(color)) : ControlPaint.Dark(ToGdiColor(color));
        var darkColor = isRaised ? ControlPaint.Dark(ToGdiColor(color)) : ControlPaint.Light(ToGdiColor(color));

        using var lightPen = new Pen(lightColor, depth);
        using var darkPen = new Pen(darkColor, depth);

        // Top-left edges (light)
        _graphics.DrawLine(lightPen, rect.X, rect.Y, rect.Right - 1, rect.Y);
        _graphics.DrawLine(lightPen, rect.X, rect.Y, rect.X, rect.Bottom - 1);

        // Bottom-right edges (dark)
        _graphics.DrawLine(darkPen, rect.X, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1);
        _graphics.DrawLine(darkPen, rect.Right - 1, rect.Y, rect.Right - 1, rect.Bottom - 1);

        // For Amiga MUI style, add inner bevel
        if (bevel == BevelStyle.AmigaMUI && depth > 1)
        {
            var innerRect = new DuskRect(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
            using var innerLightPen = new Pen(ControlPaint.LightLight(ToGdiColor(color)));
            using var innerDarkPen = new Pen(ControlPaint.DarkDark(ToGdiColor(color)));

            _graphics.DrawLine(innerLightPen, innerRect.X, innerRect.Y, innerRect.Right - 1, innerRect.Y);
            _graphics.DrawLine(innerLightPen, innerRect.X, innerRect.Y, innerRect.X, innerRect.Bottom - 1);
            _graphics.DrawLine(innerDarkPen, innerRect.X, innerRect.Bottom - 1, innerRect.Right - 1, innerRect.Bottom - 1);
            _graphics.DrawLine(innerDarkPen, innerRect.Right - 1, innerRect.Y, innerRect.Right - 1, innerRect.Bottom - 1);
        }
    }

    public void DrawText(string text, DuskPoint position, DuskFont font, DuskColor color)
    {
        if (_graphics == null || string.IsNullOrEmpty(text)) return;

        var gdiFont = GetOrCreateFont(font);
        using var brush = new SolidBrush(ToGdiColor(color));

        _graphics.DrawString(text, gdiFont, brush, position.X, position.Y);
    }

    public void DrawLine(DuskPoint start, DuskPoint end, DuskColor color, int thickness = 1)
    {
        if (_graphics == null) return;

        using var pen = new Pen(ToGdiColor(color), thickness);
        _graphics.DrawLine(pen, start.X, start.Y, end.X, end.Y);
    }

    public void DrawImage(DuskImage image, DuskRect destination)
    {
        if (_graphics == null) return;

        // Convert DUSK image to GDI+ Bitmap
        using var bitmap = new Bitmap(image.Width, image.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, image.Width, image.Height),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        System.Runtime.InteropServices.Marshal.Copy(image.PixelData, 0, bitmapData.Scan0, image.PixelData.Length);
        bitmap.UnlockBits(bitmapData);

        _graphics.DrawImage(bitmap,
            new Rectangle(destination.X, destination.Y, destination.Width, destination.Height));
    }

    public void SetClipRegion(DuskRect? region)
    {
        if (_graphics == null) return;

        _clipRegion = region;

        if (region.HasValue)
        {
            _graphics.SetClip(new Rectangle(
                region.Value.X, region.Value.Y,
                region.Value.Width, region.Value.Height));
        }
        else
        {
            _graphics.ResetClip();
        }
    }

    public DuskSize MeasureText(string text, DuskFont font)
    {
        if (string.IsNullOrEmpty(text) || _graphics == null) return DuskSize.Zero;

        var gdiFont = GetOrCreateFont(font);
        var size = _graphics.MeasureString(text, gdiFont);

        return new DuskSize((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
    }

    public void Present()
    {
        // In actual use, this would paint to a control
    }

    public Bitmap? GetBackBuffer() => _backBuffer;

    public void RenderTo(Graphics target)
    {
        if (_backBuffer != null)
        {
            target.DrawImage(_backBuffer, 0, 0);
        }
    }

    private Font GetOrCreateFont(DuskFont font)
    {
        if (_fontCache.TryGetValue(font, out var cached))
            return cached;

        var style = FontStyle.Regular;
        if (font.Style.HasFlag(DuskFontStyle.Bold)) style |= FontStyle.Bold;
        if (font.Style.HasFlag(DuskFontStyle.Italic)) style |= FontStyle.Italic;
        if (font.Style.HasFlag(DuskFontStyle.Underline)) style |= FontStyle.Underline;

        var gdiFont = new Font(font.Family, font.Size, style);
        _fontCache[font] = gdiFont;
        return gdiFont;
    }

    private static Color ToGdiColor(DuskColor color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public void Dispose()
    {
        foreach (var font in _fontCache.Values)
        {
            font.Dispose();
        }
        _fontCache.Clear();

        _graphics?.Dispose();
        _backBuffer?.Dispose();
    }
}
