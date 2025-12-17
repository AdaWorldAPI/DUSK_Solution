namespace DUSK.Theme.Presets;

using DUSK.Core;

/// <summary>
/// Modern flat design theme.
/// Clean, minimal appearance with no bevels.
/// </summary>
public class ModernFlat : ThemeProfile
{
    private ModernFlat() : base("Modern Flat", ThemeStyle.ModernFlat)
    {
    }

    protected override void InitializeDefaults()
    {
        base.InitializeDefaults();

        // Modern flat palette
        var bgWhite = new DuskColor(250, 250, 250);
        var surfaceWhite = DuskColor.White;
        var primaryBlue = new DuskColor(0, 120, 212);
        var textDark = new DuskColor(32, 32, 32);
        var textLight = new DuskColor(96, 96, 96);
        var borderGray = new DuskColor(220, 220, 220);
        var accentTeal = new DuskColor(0, 168, 168);

        // Base colors
        SetColor(ThemeColor.Background, bgWhite);
        SetColor(ThemeColor.Foreground, textDark);
        SetColor(ThemeColor.Primary, primaryBlue);
        SetColor(ThemeColor.Secondary, textLight);
        SetColor(ThemeColor.Accent, accentTeal);
        SetColor(ThemeColor.Border, borderGray);
        SetColor(ThemeColor.BorderLight, borderGray);
        SetColor(ThemeColor.BorderDark, new DuskColor(180, 180, 180));

        // Button colors - flat style
        SetColor(ThemeColor.ButtonFace, primaryBlue);
        SetColor(ThemeColor.ButtonText, DuskColor.White);
        SetColor(ThemeColor.ButtonFace, ElementState.Hover, new DuskColor(0, 100, 180));
        SetColor(ThemeColor.ButtonFace, ElementState.Pressed, new DuskColor(0, 80, 150));
        SetColor(ThemeColor.ButtonFace, ElementState.Focused, new DuskColor(0, 110, 200));
        SetColor(ThemeColor.ButtonFace, ElementState.Disabled, new DuskColor(200, 200, 200));
        SetColor(ThemeColor.ButtonText, ElementState.Disabled, new DuskColor(160, 160, 160));

        // Window colors
        SetColor(ThemeColor.WindowBackground, surfaceWhite);
        SetColor(ThemeColor.WindowTitle, primaryBlue);
        SetColor(ThemeColor.WindowTitleText, DuskColor.White);

        // Input colors
        SetColor(ThemeColor.InputBackground, surfaceWhite);
        SetColor(ThemeColor.InputText, textDark);
        SetColor(ThemeColor.InputBorder, borderGray);
        SetColor(ThemeColor.InputBackground, ElementState.Focused, surfaceWhite);
        SetColor(ThemeColor.InputBorder, ElementState.Focused, primaryBlue);

        // Selection
        SetColor(ThemeColor.SelectionBackground, primaryBlue);
        SetColor(ThemeColor.SelectionText, DuskColor.White);

        // States
        SetColor(ThemeColor.DisabledBackground, new DuskColor(245, 245, 245));
        SetColor(ThemeColor.DisabledText, new DuskColor(180, 180, 180));
        SetColor(ThemeColor.ErrorBackground, new DuskColor(253, 237, 237));
        SetColor(ThemeColor.ErrorText, new DuskColor(197, 68, 68));
        SetColor(ThemeColor.SuccessBackground, new DuskColor(237, 253, 237));
        SetColor(ThemeColor.SuccessText, new DuskColor(68, 140, 68));
        SetColor(ThemeColor.WarningBackground, new DuskColor(255, 250, 235));
        SetColor(ThemeColor.WarningText, new DuskColor(180, 140, 40));

        // Flat - no bevels
        SetBevel(ThemeBevelRole.Button, BevelStyle.None);
        SetBevel(ThemeBevelRole.ButtonPressed, BevelStyle.None);
        SetBevel(ThemeBevelRole.Input, BevelStyle.None);
        SetBevel(ThemeBevelRole.Panel, BevelStyle.None);
        SetBevel(ThemeBevelRole.Window, BevelStyle.None);

        // Metrics - modern sizing
        SetMetric(ThemeMetric.BevelDepth, 0);
        SetMetric(ThemeMetric.BorderRadius, 4);
        SetMetric(ThemeMetric.BorderWidth, 1);
        SetMetric(ThemeMetric.ButtonPadding, 12);
        SetMetric(ThemeMetric.InputPadding, 8);
        SetMetric(ThemeMetric.ElementSpacing, 8);

        // Modern fonts
        SetFont(ThemeFontRole.Default, new DuskFont("Segoe UI", 9));
        SetFont(ThemeFontRole.Title, new DuskFont("Segoe UI Light", 24));
        SetFont(ThemeFontRole.Heading, new DuskFont("Segoe UI Semibold", 14));
        SetFont(ThemeFontRole.Button, new DuskFont("Segoe UI", 9));
        SetFont(ThemeFontRole.Monospace, new DuskFont("Cascadia Code", 10));
    }

    public static ModernFlat Create() => new();
}
