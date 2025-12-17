namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Progress bar with Amiga MUI styling.
/// Supports determinate, indeterminate, and segmented modes.
/// </summary>
public class UIProgressBar : UIElementBase
{
    private float _value;
    private float _minimum;
    private float _maximum = 100f;
    private float _marqueePosition;
    private ProgressBarStyle _style = ProgressBarStyle.Continuous;

    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (Math.Abs(_value - clamped) < 0.001f) return;
            _value = clamped;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public float Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_value < _minimum) _value = _minimum;
        }
    }

    public float Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_value > _maximum) _value = _maximum;
        }
    }

    public float Percentage => (_maximum - _minimum) > 0
        ? (_value - _minimum) / (_maximum - _minimum)
        : 0;

    public ProgressBarStyle Style
    {
        get => _style;
        set => _style = value;
    }

    public bool ShowText { get; set; } = true;
    public string? CustomText { get; set; }
    public int SegmentCount { get; set; } = 10;
    public int SegmentGap { get; set; } = 2;
    public float MarqueeSpeed { get; set; } = 200f;

    public DuskColor? BarColor { get; set; }
    public DuskColor? BackgroundColor { get; set; }

    public event EventHandler? ValueChanged;

    public UIProgressBar(string? id = null) : base(id)
    {
        Bounds = new DuskRect(0, 0, 200, 20);
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_style == ProgressBarStyle.Marquee)
        {
            _marqueePosition += MarqueeSpeed * deltaTime;
            if (_marqueePosition > Bounds.Width + 50)
            {
                _marqueePosition = -50;
            }
        }
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        // Draw background
        var bgColor = BackgroundColor ?? theme?.GetColor(ThemeColor.InputBackground, state) ?? DuskColor.White;
        renderer.DrawRectangleBeveled(Bounds, bgColor, BevelStyle.Sunken, 1);

        // Calculate fill area
        var innerBounds = Bounds.Inflate(-2, -2);
        var barColor = BarColor ?? theme?.GetColor(ThemeColor.Primary, state) ?? DuskColor.AmigaBlue;

        switch (_style)
        {
            case ProgressBarStyle.Continuous:
                RenderContinuous(renderer, innerBounds, barColor);
                break;

            case ProgressBarStyle.Segmented:
                RenderSegmented(renderer, innerBounds, barColor);
                break;

            case ProgressBarStyle.Marquee:
                RenderMarquee(renderer, innerBounds, barColor);
                break;
        }

        // Draw text
        if (ShowText && _style != ProgressBarStyle.Marquee)
        {
            var text = CustomText ?? $"{(int)(Percentage * 100)}%";
            var font = theme?.GetFont(ThemeFontRole.Default) ?? DuskFont.Default;
            var textSize = renderer.MeasureText(text, font);
            var textX = Bounds.X + (Bounds.Width - textSize.Width) / 2;
            var textY = Bounds.Y + (Bounds.Height - textSize.Height) / 2;

            // Draw text with contrasting color
            var textColor = Percentage > 0.5f ? DuskColor.White : DuskColor.Black;
            renderer.DrawText(text, new DuskPoint(textX, textY), font, textColor);
        }
    }

    private void RenderContinuous(IRenderer renderer, DuskRect bounds, DuskColor color)
    {
        var fillWidth = (int)(bounds.Width * Percentage);
        if (fillWidth > 0)
        {
            var fillRect = new DuskRect(bounds.X, bounds.Y, fillWidth, bounds.Height);
            renderer.DrawRectangle(fillRect, color);
        }
    }

    private void RenderSegmented(IRenderer renderer, DuskRect bounds, DuskColor color)
    {
        var segmentWidth = (bounds.Width - (SegmentCount - 1) * SegmentGap) / SegmentCount;
        var filledSegments = (int)(SegmentCount * Percentage);

        for (int i = 0; i < filledSegments; i++)
        {
            var x = bounds.X + i * (segmentWidth + SegmentGap);
            var segmentRect = new DuskRect((int)x, bounds.Y, (int)segmentWidth, bounds.Height);
            renderer.DrawRectangle(segmentRect, color);
        }
    }

    private void RenderMarquee(IRenderer renderer, DuskRect bounds, DuskColor color)
    {
        var marqueeWidth = 50;
        var x = (int)(bounds.X + _marqueePosition);

        // Draw marquee block
        var marqueeRect = new DuskRect(
            Math.Max(bounds.X, x),
            bounds.Y,
            Math.Min(marqueeWidth, bounds.Right - x),
            bounds.Height
        );

        if (marqueeRect.Width > 0)
        {
            renderer.DrawRectangle(marqueeRect, color);
        }
    }

    public void Increment(float amount = 1)
    {
        Value += amount;
    }
}

public enum ProgressBarStyle
{
    Continuous,
    Segmented,
    Marquee
}
