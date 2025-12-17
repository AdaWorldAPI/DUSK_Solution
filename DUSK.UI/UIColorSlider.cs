namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// A color slider with immediate visual feedback.
/// Drag anywhere on the track to change the value.
/// MUI-style: no buttons, just click and drag.
/// </summary>
public class UIColorSlider : UIElementBase
{
    private float _value;
    private bool _isDragging;
    private ColorChannel _channel;

    /// <summary>
    /// Current value (0.0 - 1.0).
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var newValue = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_value - newValue) > 0.001f)
            {
                _value = newValue;
                ValueChanged?.Invoke(this, _value);
            }
        }
    }

    /// <summary>
    /// Which color channel this slider controls.
    /// </summary>
    public ColorChannel Channel
    {
        get => _channel;
        set => _channel = value;
    }

    /// <summary>
    /// Base color for the gradient display.
    /// </summary>
    public DuskColor BaseColor { get; set; } = DuskColor.White;

    /// <summary>
    /// Show the value as text.
    /// </summary>
    public bool ShowValue { get; set; } = true;

    public event EventHandler<float>? ValueChanged;

    public UIColorSlider(ColorChannel channel, string? id = null) : base(id)
    {
        _channel = channel;
        Bounds = new DuskRect(0, 0, 200, 24);
    }

    public override void HandleMouseDown(MouseEventArgs args)
    {
        base.HandleMouseDown(args);
        if (!Enabled) return;

        _isDragging = true;
        UpdateValueFromMouse(args.Position);
    }

    public override void HandleMouseUp(MouseEventArgs args)
    {
        base.HandleMouseUp(args);
        _isDragging = false;
    }

    public override void HandleMouseLeave(MouseEventArgs args)
    {
        base.HandleMouseLeave(args);
        _isDragging = false;
    }

    protected override void OnUpdate(float deltaTime)
    {
        // If dragging, continuously update from mouse
        // (In real impl, would get current mouse pos from InputManager)
    }

    private void UpdateValueFromMouse(DuskPoint pos)
    {
        int trackStart = Bounds.X + 4;
        int trackWidth = Bounds.Width - 8;
        float t = (pos.X - trackStart) / (float)trackWidth;
        Value = Math.Clamp(t, 0f, 1f);
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Track background with gradient
        var trackBounds = new DuskRect(Bounds.X + 2, Bounds.Y + 6, Bounds.Width - 4, Bounds.Height - 12);

        // Draw gradient based on channel
        for (int x = 0; x < trackBounds.Width; x++)
        {
            float t = x / (float)trackBounds.Width;
            var color = GetGradientColor(t);
            renderer.DrawLine(
                new DuskPoint(trackBounds.X + x, trackBounds.Y),
                new DuskPoint(trackBounds.X + x, trackBounds.Y + trackBounds.Height),
                color
            );
        }

        // Border
        renderer.DrawRect(trackBounds, new DuskColor(60, 60, 60));

        // Thumb position
        int thumbX = trackBounds.X + (int)(_value * trackBounds.Width);
        var thumbBounds = new DuskRect(thumbX - 4, Bounds.Y + 2, 8, Bounds.Height - 4);

        // Thumb with bevel
        var thumbColor = _isDragging ? new DuskColor(255, 255, 255) : new DuskColor(200, 200, 200);
        renderer.FillRect(thumbBounds, thumbColor);
        // Highlight
        renderer.DrawLine(new DuskPoint(thumbBounds.X, thumbBounds.Y), new DuskPoint(thumbBounds.X + thumbBounds.Width, thumbBounds.Y), DuskColor.White);
        renderer.DrawLine(new DuskPoint(thumbBounds.X, thumbBounds.Y), new DuskPoint(thumbBounds.X, thumbBounds.Y + thumbBounds.Height), DuskColor.White);
        // Shadow
        renderer.DrawLine(new DuskPoint(thumbBounds.X, thumbBounds.Y + thumbBounds.Height), new DuskPoint(thumbBounds.X + thumbBounds.Width, thumbBounds.Y + thumbBounds.Height), new DuskColor(80, 80, 80));
        renderer.DrawLine(new DuskPoint(thumbBounds.X + thumbBounds.Width, thumbBounds.Y), new DuskPoint(thumbBounds.X + thumbBounds.Width, thumbBounds.Y + thumbBounds.Height), new DuskColor(80, 80, 80));

        // Value text
        if (ShowValue)
        {
            int displayValue = (int)(_value * 255);
            var font = new DuskFont("Default", 10);
            string text = _channel switch
            {
                ColorChannel.Red => $"R:{displayValue}",
                ColorChannel.Green => $"G:{displayValue}",
                ColorChannel.Blue => $"B:{displayValue}",
                ColorChannel.Alpha => $"A:{displayValue}",
                ColorChannel.Hue => $"H:{(int)(_value * 360)}°",
                ColorChannel.Saturation => $"S:{displayValue}",
                ColorChannel.Brightness => $"V:{displayValue}",
                _ => $"{displayValue}"
            };
            renderer.DrawText(text, new DuskPoint(Bounds.X + Bounds.Width + 4, Bounds.Y + 4), font, DuskColor.White);
        }
    }

    private DuskColor GetGradientColor(float t)
    {
        byte v = (byte)(t * 255);
        return _channel switch
        {
            ColorChannel.Red => new DuskColor(v, BaseColor.G, BaseColor.B),
            ColorChannel.Green => new DuskColor(BaseColor.R, v, BaseColor.B),
            ColorChannel.Blue => new DuskColor(BaseColor.R, BaseColor.G, v),
            ColorChannel.Alpha => new DuskColor(BaseColor.R, BaseColor.G, BaseColor.B, v),
            ColorChannel.Hue => HsvToRgb(t, 1f, 1f),
            ColorChannel.Saturation => HsvToRgb(0f, t, 1f),
            ColorChannel.Brightness => HsvToRgb(0f, 0f, t),
            _ => new DuskColor(v, v, v)
        };
    }

    private static DuskColor HsvToRgb(float h, float s, float v)
    {
        int hi = (int)(h * 6) % 6;
        float f = h * 6 - hi;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);

        float r, g, b;
        switch (hi)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }

        return new DuskColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}

public enum ColorChannel
{
    Red,
    Green,
    Blue,
    Alpha,
    Hue,
    Saturation,
    Brightness
}
