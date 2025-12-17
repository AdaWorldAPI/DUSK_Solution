namespace DUSK.Studio;

using DUSK.Core;
using DUSK.UI;
using DUSK.Theme;

/// <summary>
/// MUI-style live theme editor.
/// Philosophy: Immediate feedback, direct manipulation, no modal dialogs.
/// Click any color to edit it. Drag sliders to see changes instantly.
/// Like Magic User Interface preferences or Mario World Editor - just click and tweak.
/// </summary>
public class LiveThemeEditor : UIElementBase
{
    private ThemeProfile _editingTheme;
    private ThemeColorTarget _selectedTarget = ThemeColorTarget.WindowBackground;
    private readonly List<ColorSwatch> _swatches = new();
    private readonly List<UIColorSlider> _rgbSliders = new();
    private readonly UIWaveformDisplay? _previewWaveform;

    // Layout zones
    private DuskRect _swatchZone;
    private DuskRect _sliderZone;
    private DuskRect _previewZone;
    private DuskRect _presetsZone;

    public ThemeProfile EditingTheme
    {
        get => _editingTheme;
        set
        {
            _editingTheme = value;
            RefreshSwatches();
        }
    }

    public event EventHandler<ThemeProfile>? ThemeChanged;

    public LiveThemeEditor(string? id = null) : base(id)
    {
        _editingTheme = ThemeManager.Instance.CurrentTheme ?? CreateDefaultTheme();
        Bounds = new DuskRect(0, 0, 400, 500);

        InitializeSliders();
        InitializeSwatches();
        LayoutComponents();
    }

    private void InitializeSliders()
    {
        // RGB sliders - immediate response
        var redSlider = new UIColorSlider(ColorChannel.Red) { Name = "Red" };
        var greenSlider = new UIColorSlider(ColorChannel.Green) { Name = "Green" };
        var blueSlider = new UIColorSlider(ColorChannel.Blue) { Name = "Blue" };

        redSlider.ValueChanged += (s, v) => UpdateSelectedColor();
        greenSlider.ValueChanged += (s, v) => UpdateSelectedColor();
        blueSlider.ValueChanged += (s, v) => UpdateSelectedColor();

        _rgbSliders.Add(redSlider);
        _rgbSliders.Add(greenSlider);
        _rgbSliders.Add(blueSlider);

        foreach (var slider in _rgbSliders)
        {
            AddChild(slider);
        }
    }

    private void InitializeSwatches()
    {
        // Create clickable color swatches for each theme color
        var targets = Enum.GetValues<ThemeColorTarget>();
        foreach (var target in targets)
        {
            var swatch = new ColorSwatch(target)
            {
                Name = target.ToString()
            };
            swatch.Selected += OnSwatchSelected;
            _swatches.Add(swatch);
            AddChild(swatch);
        }
    }

    private void LayoutComponents()
    {
        int padding = 8;
        int swatchSize = 32;
        int swatchesPerRow = 6;

        // Swatches at top - grid of clickable colors
        _swatchZone = new DuskRect(
            Bounds.X + padding,
            Bounds.Y + padding + 20,
            Bounds.Width - padding * 2,
            ((_swatches.Count / swatchesPerRow) + 1) * (swatchSize + 4)
        );

        for (int i = 0; i < _swatches.Count; i++)
        {
            int row = i / swatchesPerRow;
            int col = i % swatchesPerRow;
            _swatches[i].Bounds = new DuskRect(
                _swatchZone.X + col * (swatchSize + 4),
                _swatchZone.Y + row * (swatchSize + 4),
                swatchSize,
                swatchSize
            );
        }

        // Sliders in middle
        int sliderY = _swatchZone.Y + _swatchZone.Height + padding;
        _sliderZone = new DuskRect(
            Bounds.X + padding,
            sliderY,
            Bounds.Width - padding * 2 - 50,
            90
        );

        for (int i = 0; i < _rgbSliders.Count; i++)
        {
            _rgbSliders[i].Bounds = new DuskRect(
                _sliderZone.X,
                _sliderZone.Y + i * 28,
                _sliderZone.Width,
                24
            );
        }

        // Preview zone
        int previewY = _sliderZone.Y + _sliderZone.Height + padding;
        _previewZone = new DuskRect(
            Bounds.X + padding,
            previewY,
            Bounds.Width - padding * 2,
            120
        );

        // Presets at bottom
        _presetsZone = new DuskRect(
            Bounds.X + padding,
            _previewZone.Y + _previewZone.Height + padding,
            Bounds.Width - padding * 2,
            60
        );
    }

    private void OnSwatchSelected(object? sender, ThemeColorTarget target)
    {
        _selectedTarget = target;

        // Update sliders to reflect selected color
        var color = GetThemeColor(target);
        _rgbSliders[0].Value = color.R / 255f;
        _rgbSliders[1].Value = color.G / 255f;
        _rgbSliders[2].Value = color.B / 255f;

        // Update slider base colors for accurate gradient preview
        _rgbSliders[0].BaseColor = color;
        _rgbSliders[1].BaseColor = color;
        _rgbSliders[2].BaseColor = color;
    }

    private void UpdateSelectedColor()
    {
        var newColor = new DuskColor(
            (byte)(_rgbSliders[0].Value * 255),
            (byte)(_rgbSliders[1].Value * 255),
            (byte)(_rgbSliders[2].Value * 255)
        );

        SetThemeColor(_selectedTarget, newColor);

        // Update the swatch
        var swatch = _swatches.FirstOrDefault(s => s.Target == _selectedTarget);
        if (swatch != null)
        {
            swatch.Color = newColor;
        }

        // Immediate feedback - apply to current theme
        ThemeManager.Instance.CurrentTheme = _editingTheme;
        ThemeChanged?.Invoke(this, _editingTheme);
    }

    private void RefreshSwatches()
    {
        foreach (var swatch in _swatches)
        {
            swatch.Color = GetThemeColor(swatch.Target);
        }
    }

    private DuskColor GetThemeColor(ThemeColorTarget target)
    {
        var themeColor = TargetToThemeColor(target);
        return _editingTheme.GetColor(themeColor);
    }

    private void SetThemeColor(ThemeColorTarget target, DuskColor color)
    {
        var themeColor = TargetToThemeColor(target);
        _editingTheme.SetColor(themeColor, color);
    }

    private static ThemeColor TargetToThemeColor(ThemeColorTarget target)
    {
        return target switch
        {
            ThemeColorTarget.WindowBackground => ThemeColor.WindowBackground,
            ThemeColorTarget.ControlBackground => ThemeColor.ControlBackground,
            ThemeColorTarget.ControlForeground => ThemeColor.ControlForeground,
            ThemeColorTarget.AccentPrimary => ThemeColor.Accent,
            ThemeColorTarget.AccentSecondary => ThemeColor.AccentDark,
            ThemeColorTarget.BorderLight => ThemeColor.BorderLight,
            ThemeColorTarget.BorderDark => ThemeColor.BorderDark,
            ThemeColorTarget.TextPrimary => ThemeColor.Text,
            ThemeColorTarget.TextSecondary => ThemeColor.TextDisabled,
            ThemeColorTarget.SelectionBackground => ThemeColor.Selection,
            ThemeColorTarget.ErrorColor => ThemeColor.Error,
            ThemeColorTarget.WarningColor => ThemeColor.Warning,
            ThemeColorTarget.SuccessColor => ThemeColor.Success,
            _ => ThemeColor.Text
        };
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Panel background with subtle gradient
        renderer.FillRect(Bounds, new DuskColor(50, 50, 60));

        // Title bar
        var titleBar = new DuskRect(Bounds.X, Bounds.Y, Bounds.Width, 20);
        renderer.FillRect(titleBar, new DuskColor(70, 70, 90));
        renderer.DrawText("Theme Editor", new DuskPoint(Bounds.X + 8, Bounds.Y + 3),
            new DuskFont("Default", 12), DuskColor.White);

        // Section labels
        renderer.DrawText("Colors", new DuskPoint(_swatchZone.X, _swatchZone.Y - 14),
            new DuskFont("Default", 10), new DuskColor(180, 180, 200));

        renderer.DrawText($"Edit: {_selectedTarget}", new DuskPoint(_sliderZone.X, _sliderZone.Y - 14),
            new DuskFont("Default", 10), new DuskColor(180, 180, 200));

        // Preview section
        renderer.DrawText("Preview", new DuskPoint(_previewZone.X, _previewZone.Y - 14),
            new DuskFont("Default", 10), new DuskColor(180, 180, 200));
        RenderPreview(renderer);

        // Presets section
        renderer.DrawText("Presets (click to apply)", new DuskPoint(_presetsZone.X, _presetsZone.Y - 14),
            new DuskFont("Default", 10), new DuskColor(180, 180, 200));
        RenderPresets(renderer);

        // Border
        renderer.DrawRect(Bounds, new DuskColor(100, 100, 120));
    }

    private void RenderPreview(IRenderer renderer)
    {
        // Mini preview of a button, text field, and window
        var previewBg = new DuskRect(_previewZone.X, _previewZone.Y, _previewZone.Width, _previewZone.Height);
        renderer.FillRect(previewBg, GetThemeColor(ThemeColorTarget.WindowBackground));
        renderer.DrawRect(previewBg, GetThemeColor(ThemeColorTarget.BorderDark));

        // Preview button
        var btnBounds = new DuskRect(_previewZone.X + 10, _previewZone.Y + 10, 80, 28);
        renderer.FillRect(btnBounds, GetThemeColor(ThemeColorTarget.ControlBackground));
        renderer.DrawLine(new DuskPoint(btnBounds.X, btnBounds.Y), new DuskPoint(btnBounds.X + btnBounds.Width, btnBounds.Y), GetThemeColor(ThemeColorTarget.BorderLight));
        renderer.DrawLine(new DuskPoint(btnBounds.X, btnBounds.Y), new DuskPoint(btnBounds.X, btnBounds.Y + btnBounds.Height), GetThemeColor(ThemeColorTarget.BorderLight));
        renderer.DrawLine(new DuskPoint(btnBounds.X, btnBounds.Y + btnBounds.Height), new DuskPoint(btnBounds.X + btnBounds.Width, btnBounds.Y + btnBounds.Height), GetThemeColor(ThemeColorTarget.BorderDark));
        renderer.DrawLine(new DuskPoint(btnBounds.X + btnBounds.Width, btnBounds.Y), new DuskPoint(btnBounds.X + btnBounds.Width, btnBounds.Y + btnBounds.Height), GetThemeColor(ThemeColorTarget.BorderDark));
        renderer.DrawText("Button", new DuskPoint(btnBounds.X + 16, btnBounds.Y + 7), new DuskFont("Default", 11), GetThemeColor(ThemeColorTarget.ControlForeground));

        // Preview text field
        var txtBounds = new DuskRect(_previewZone.X + 100, _previewZone.Y + 10, 120, 24);
        renderer.FillRect(txtBounds, DuskColor.White);
        renderer.DrawRect(txtBounds, GetThemeColor(ThemeColorTarget.BorderDark));
        renderer.DrawText("Text input", new DuskPoint(txtBounds.X + 4, txtBounds.Y + 5), new DuskFont("Default", 11), GetThemeColor(ThemeColorTarget.TextPrimary));

        // Preview labels
        renderer.DrawText("Primary text", new DuskPoint(_previewZone.X + 10, _previewZone.Y + 50), new DuskFont("Default", 12), GetThemeColor(ThemeColorTarget.TextPrimary));
        renderer.DrawText("Secondary text", new DuskPoint(_previewZone.X + 10, _previewZone.Y + 68), new DuskFont("Default", 10), GetThemeColor(ThemeColorTarget.TextSecondary));

        // Accent color preview
        renderer.FillRect(new DuskRect(_previewZone.X + 10, _previewZone.Y + 90, 60, 20), GetThemeColor(ThemeColorTarget.AccentPrimary));
        renderer.FillRect(new DuskRect(_previewZone.X + 75, _previewZone.Y + 90, 60, 20), GetThemeColor(ThemeColorTarget.AccentSecondary));

        // Status colors
        renderer.FillRect(new DuskRect(_previewZone.X + 150, _previewZone.Y + 90, 30, 20), GetThemeColor(ThemeColorTarget.SuccessColor));
        renderer.FillRect(new DuskRect(_previewZone.X + 185, _previewZone.Y + 90, 30, 20), GetThemeColor(ThemeColorTarget.WarningColor));
        renderer.FillRect(new DuskRect(_previewZone.X + 220, _previewZone.Y + 90, 30, 20), GetThemeColor(ThemeColorTarget.ErrorColor));
    }

    private void RenderPresets(IRenderer renderer)
    {
        // Quick preset buttons - click to instantly apply
        var presets = new[]
        {
            ("MUI Classic", new DuskColor(170, 170, 170)),
            ("MUI Royale", new DuskColor(60, 80, 120)),
            ("Modern Flat", new DuskColor(245, 245, 250)),
            ("Win32", new DuskColor(212, 208, 200)),
            ("Dark", new DuskColor(30, 30, 35))
        };

        int btnWidth = 70;
        int btnHeight = 24;
        int gap = 6;

        for (int i = 0; i < presets.Length; i++)
        {
            var (name, color) = presets[i];
            var btnBounds = new DuskRect(
                _presetsZone.X + i * (btnWidth + gap),
                _presetsZone.Y,
                btnWidth,
                btnHeight
            );

            // Button with theme color hint
            renderer.FillRect(btnBounds, color);
            var textColor = (color.R + color.G + color.B) > 380 ? DuskColor.Black : DuskColor.White;
            renderer.DrawText(name, new DuskPoint(btnBounds.X + 4, btnBounds.Y + 5),
                new DuskFont("Default", 9), textColor);
            renderer.DrawRect(btnBounds, new DuskColor(80, 80, 80));
        }
    }

    private static ThemeProfile CreateDefaultTheme()
    {
        var theme = new ThemeProfile("Custom", ThemeStyle.AmigaMUI);
        theme.SetColor(ThemeColor.WindowBackground, new DuskColor(170, 170, 170));
        theme.SetColor(ThemeColor.ControlBackground, new DuskColor(190, 190, 190));
        theme.SetColor(ThemeColor.ControlForeground, DuskColor.Black);
        theme.SetColor(ThemeColor.Accent, new DuskColor(100, 130, 180));
        theme.SetColor(ThemeColor.AccentDark, new DuskColor(180, 130, 100));
        theme.SetColor(ThemeColor.BorderLight, DuskColor.White);
        theme.SetColor(ThemeColor.BorderDark, new DuskColor(80, 80, 80));
        theme.SetColor(ThemeColor.Text, DuskColor.Black);
        theme.SetColor(ThemeColor.TextDisabled, new DuskColor(60, 60, 60));
        theme.SetColor(ThemeColor.Selection, new DuskColor(100, 130, 180));
        theme.SetColor(ThemeColor.Error, new DuskColor(200, 60, 60));
        theme.SetColor(ThemeColor.Warning, new DuskColor(200, 180, 60));
        theme.SetColor(ThemeColor.Success, new DuskColor(60, 180, 60));
        return theme;
    }
}

/// <summary>
/// A clickable color swatch in the theme editor.
/// </summary>
internal class ColorSwatch : UIElementBase
{
    public ThemeColorTarget Target { get; }
    public DuskColor Color { get; set; }
    public bool IsSelected { get; set; }

    public event EventHandler<ThemeColorTarget>? Selected;

    public ColorSwatch(ThemeColorTarget target, string? id = null) : base(id)
    {
        Target = target;
        Bounds = new DuskRect(0, 0, 32, 32);
    }

    public override void HandleMouseDown(MouseEventArgs args)
    {
        base.HandleMouseDown(args);
        IsSelected = true;
        Selected?.Invoke(this, Target);
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Swatch color
        renderer.FillRect(Bounds, Color);

        // Selection indicator
        if (IsSelected)
        {
            renderer.DrawRect(Bounds, DuskColor.White);
            renderer.DrawRect(new DuskRect(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 2, Bounds.Height - 2), DuskColor.Black);
        }
        else
        {
            renderer.DrawRect(Bounds, new DuskColor(60, 60, 60));
        }

        // Tooltip-style label on hover
        if (IsHovered)
        {
            var font = new DuskFont("Default", 9);
            var label = Target.ToString();
            renderer.DrawText(label, new DuskPoint(Bounds.X, Bounds.Y + Bounds.Height + 2), font, DuskColor.White);
        }
    }
}

/// <summary>
/// Theme color targets that can be edited.
/// </summary>
public enum ThemeColorTarget
{
    WindowBackground,
    ControlBackground,
    ControlForeground,
    AccentPrimary,
    AccentSecondary,
    BorderLight,
    BorderDark,
    TextPrimary,
    TextSecondary,
    SelectionBackground,
    ErrorColor,
    WarningColor,
    SuccessColor
}
