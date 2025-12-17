namespace DUSK.Theme.Presets;

using DUSK.Core;

/// <summary>
/// Classic Amiga MUI 3.x theme.
/// Authentic gray color scheme with beveled 3D appearance.
/// </summary>
public class AmigaMUIClassic : ThemeProfile
{
    private AmigaMUIClassic() : base("Amiga MUI Classic", ThemeStyle.AmigaMUI)
    {
    }

    protected override void InitializeDefaults()
    {
        base.InitializeDefaults();

        // Classic Amiga gray palette
        var bgGray = new DuskColor(170, 170, 170);       // Standard Workbench gray
        var lightGray = new DuskColor(204, 204, 204);    // Highlight
        var darkGray = new DuskColor(102, 102, 102);     // Shadow
        var black = DuskColor.Black;
        var white = DuskColor.White;
        var blue = new DuskColor(0, 85, 170);            // Amiga blue
        var orange = new DuskColor(255, 136, 0);         // Amiga orange

        // Base colors
        SetColor(ThemeColor.Background, bgGray);
        SetColor(ThemeColor.Foreground, black);
        SetColor(ThemeColor.Primary, blue);
        SetColor(ThemeColor.Secondary, darkGray);
        SetColor(ThemeColor.Accent, orange);
        SetColor(ThemeColor.Border, black);
        SetColor(ThemeColor.BorderLight, white);
        SetColor(ThemeColor.BorderDark, darkGray);

        // Button colors
        SetColor(ThemeColor.ButtonFace, bgGray);
        SetColor(ThemeColor.ButtonText, black);
        SetColor(ThemeColor.ButtonFace, ElementState.Hover, lightGray);
        SetColor(ThemeColor.ButtonFace, ElementState.Pressed, darkGray);
        SetColor(ThemeColor.ButtonText, ElementState.Pressed, white);
        SetColor(ThemeColor.ButtonFace, ElementState.Disabled, lightGray);
        SetColor(ThemeColor.ButtonText, ElementState.Disabled, darkGray);

        // Window colors
        SetColor(ThemeColor.WindowBackground, bgGray);
        SetColor(ThemeColor.WindowTitle, blue);
        SetColor(ThemeColor.WindowTitleText, white);

        // Input colors
        SetColor(ThemeColor.InputBackground, white);
        SetColor(ThemeColor.InputText, black);
        SetColor(ThemeColor.InputBorder, darkGray);
        SetColor(ThemeColor.InputBackground, ElementState.Focused, new DuskColor(255, 255, 230));
        SetColor(ThemeColor.InputBorder, ElementState.Focused, blue);

        // Selection
        SetColor(ThemeColor.SelectionBackground, blue);
        SetColor(ThemeColor.SelectionText, white);

        // States
        SetColor(ThemeColor.DisabledBackground, lightGray);
        SetColor(ThemeColor.DisabledText, darkGray);
        SetColor(ThemeColor.ErrorBackground, new DuskColor(255, 200, 200));
        SetColor(ThemeColor.ErrorText, new DuskColor(180, 0, 0));
        SetColor(ThemeColor.SuccessBackground, new DuskColor(200, 255, 200));
        SetColor(ThemeColor.SuccessText, new DuskColor(0, 128, 0));
        SetColor(ThemeColor.WarningBackground, new DuskColor(255, 255, 200));
        SetColor(ThemeColor.WarningText, new DuskColor(180, 140, 0));

        // Amiga-style bevels
        SetBevel(ThemeBevelRole.Button, BevelStyle.AmigaMUI);
        SetBevel(ThemeBevelRole.ButtonPressed, BevelStyle.SunkenSoft);
        SetBevel(ThemeBevelRole.Input, BevelStyle.Sunken);
        SetBevel(ThemeBevelRole.Panel, BevelStyle.Etched);
        SetBevel(ThemeBevelRole.Window, BevelStyle.RaisedSoft);

        // Metrics
        SetMetric(ThemeMetric.BevelDepth, 2);
        SetMetric(ThemeMetric.BorderRadius, 0); // No rounded corners in classic Amiga
        SetMetric(ThemeMetric.ButtonPadding, 6);

        // Fonts - Topaz was the iconic Amiga font
        SetFont(ThemeFontRole.Default, new DuskFont("Topaz", 8));
        SetFont(ThemeFontRole.Title, new DuskFont("Topaz", 11, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Heading, new DuskFont("Topaz", 9, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Button, new DuskFont("Topaz", 8));
        SetFont(ThemeFontRole.Monospace, new DuskFont("Topaz", 8));
    }

    protected override void ApplyMood(ThemeMood mood)
    {
        base.ApplyMood(mood);

        var profile = MoodProfile.GetProfile(mood);

        // Apply mood to accent color
        if (profile.AccentColor.HasValue)
        {
            SetColor(ThemeColor.Accent, profile.AccentColor.Value);
            SetColor(ThemeColor.WindowTitle, profile.AccentColor.Value);
        }

        // Shift background slightly based on mood
        var baseBg = new DuskColor(170, 170, 170);
        SetColor(ThemeColor.Background, profile.Apply(baseBg));
        SetColor(ThemeColor.WindowBackground, profile.Apply(baseBg));
    }

    public static AmigaMUIClassic Create() => new();
}
