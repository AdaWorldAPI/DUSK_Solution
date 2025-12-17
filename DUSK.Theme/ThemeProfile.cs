namespace DUSK.Theme;

using DUSK.Core;

/// <summary>
/// Complete theme profile containing all colors, fonts, and metrics.
/// Base implementation of ITheme.
/// </summary>
public class ThemeProfile : ITheme
{
    private readonly Dictionary<ThemeColor, DuskColor> _colors = new();
    private readonly Dictionary<(ThemeColor, ElementState), DuskColor> _stateColors = new();
    private readonly Dictionary<ThemeFontRole, DuskFont> _fonts = new();
    private readonly Dictionary<ThemeMetric, int> _metrics = new();
    private readonly Dictionary<ThemeBevelRole, BevelStyle> _bevels = new();

    public string Name { get; }
    public ThemeStyle Style { get; }
    public ThemeMood CurrentMood { get; private set; } = ThemeMood.Neutral;

    public ThemeProfile(string name, ThemeStyle style)
    {
        Name = name;
        Style = style;
        InitializeDefaults();
    }

    protected virtual void InitializeDefaults()
    {
        // Default metrics
        SetMetric(ThemeMetric.BorderWidth, 1);
        SetMetric(ThemeMetric.BorderRadius, 0);
        SetMetric(ThemeMetric.ButtonPadding, 8);
        SetMetric(ThemeMetric.InputPadding, 4);
        SetMetric(ThemeMetric.ElementSpacing, 4);
        SetMetric(ThemeMetric.WindowPadding, 8);
        SetMetric(ThemeMetric.BevelDepth, 2);
        SetMetric(ThemeMetric.FocusWidth, 2);
        SetMetric(ThemeMetric.ScrollbarWidth, 16);
        SetMetric(ThemeMetric.MinButtonWidth, 75);
        SetMetric(ThemeMetric.MinButtonHeight, 23);

        // Default fonts
        SetFont(ThemeFontRole.Default, DuskFont.Default);
        SetFont(ThemeFontRole.Title, new DuskFont("Topaz", 12, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Heading, new DuskFont("Topaz", 10, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Body, DuskFont.Default);
        SetFont(ThemeFontRole.Caption, new DuskFont("Topaz", 7));
        SetFont(ThemeFontRole.Button, DuskFont.Default);
        SetFont(ThemeFontRole.Input, DuskFont.Default);
        SetFont(ThemeFontRole.Monospace, new DuskFont("Courier", 8));

        // Default bevels
        SetBevel(ThemeBevelRole.Button, BevelStyle.Raised);
        SetBevel(ThemeBevelRole.ButtonPressed, BevelStyle.Sunken);
        SetBevel(ThemeBevelRole.Input, BevelStyle.Sunken);
        SetBevel(ThemeBevelRole.Panel, BevelStyle.Etched);
        SetBevel(ThemeBevelRole.Window, BevelStyle.Raised);
        SetBevel(ThemeBevelRole.GroupBox, BevelStyle.Etched);
        SetBevel(ThemeBevelRole.Divider, BevelStyle.Etched);
    }

    public DuskColor GetColor(ThemeColor colorType)
    {
        return _colors.GetValueOrDefault(colorType, DuskColor.Black);
    }

    public DuskColor GetColor(ThemeColor colorType, ElementState state)
    {
        if (_stateColors.TryGetValue((colorType, state), out var color))
            return color;

        return GetColor(colorType);
    }

    public DuskFont GetFont(ThemeFontRole role)
    {
        return _fonts.GetValueOrDefault(role, DuskFont.Default);
    }

    public int GetMetric(ThemeMetric metric)
    {
        return _metrics.GetValueOrDefault(metric, 0);
    }

    public BevelStyle GetBevelStyle(ThemeBevelRole role)
    {
        return _bevels.GetValueOrDefault(role, BevelStyle.None);
    }

    public ITheme WithMood(ThemeMood mood)
    {
        var themed = Clone();
        themed.ApplyMood(mood);
        return themed;
    }

    protected virtual void ApplyMood(ThemeMood mood)
    {
        CurrentMood = mood;
        // Override in derived classes to apply mood-specific color modifications
    }

    public void SetColor(ThemeColor colorType, DuskColor color)
    {
        _colors[colorType] = color;
    }

    public void SetColor(ThemeColor colorType, ElementState state, DuskColor color)
    {
        _stateColors[(colorType, state)] = color;
    }

    public void SetFont(ThemeFontRole role, DuskFont font)
    {
        _fonts[role] = font;
    }

    public void SetMetric(ThemeMetric metric, int value)
    {
        _metrics[metric] = value;
    }

    public void SetBevel(ThemeBevelRole role, BevelStyle style)
    {
        _bevels[role] = style;
    }

    protected ThemeProfile Clone()
    {
        var clone = new ThemeProfile(Name, Style);

        foreach (var kvp in _colors)
            clone._colors[kvp.Key] = kvp.Value;

        foreach (var kvp in _stateColors)
            clone._stateColors[kvp.Key] = kvp.Value;

        foreach (var kvp in _fonts)
            clone._fonts[kvp.Key] = kvp.Value;

        foreach (var kvp in _metrics)
            clone._metrics[kvp.Key] = kvp.Value;

        foreach (var kvp in _bevels)
            clone._bevels[kvp.Key] = kvp.Value;

        return clone;
    }
}
