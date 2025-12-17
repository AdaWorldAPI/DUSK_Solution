namespace DUSK.Theme.Presets;

using DUSK.Core;

/// <summary>
/// Amiga MUI Royale theme - a more modern take on MUI.
/// Richer colors and smoother gradients while maintaining MUI aesthetics.
/// </summary>
public class AmigaMUIRoyale : ThemeProfile
{
    private AmigaMUIRoyale() : base("Amiga MUI Royale", ThemeStyle.AmigaRoyale)
    {
    }

    protected override void InitializeDefaults()
    {
        base.InitializeDefaults();

        // Royale color palette - deeper, richer colors
        var bgColor = new DuskColor(180, 185, 195);      // Slight blue tint
        var lightColor = new DuskColor(220, 225, 235);
        var darkColor = new DuskColor(90, 95, 105);
        var accentBlue = new DuskColor(60, 110, 180);    // Richer blue
        var accentGold = new DuskColor(220, 175, 55);    // Royal gold
        var black = new DuskColor(30, 30, 35);
        var white = new DuskColor(250, 250, 255);

        // Base colors
        SetColor(ThemeColor.Background, bgColor);
        SetColor(ThemeColor.Foreground, black);
        SetColor(ThemeColor.Primary, accentBlue);
        SetColor(ThemeColor.Secondary, darkColor);
        SetColor(ThemeColor.Accent, accentGold);
        SetColor(ThemeColor.Border, darkColor);
        SetColor(ThemeColor.BorderLight, white);
        SetColor(ThemeColor.BorderDark, new DuskColor(60, 65, 75));

        // Button colors with gradient effect simulation
        SetColor(ThemeColor.ButtonFace, new DuskColor(185, 190, 200));
        SetColor(ThemeColor.ButtonText, black);
        SetColor(ThemeColor.ButtonFace, ElementState.Hover, new DuskColor(200, 205, 215));
        SetColor(ThemeColor.ButtonFace, ElementState.Pressed, new DuskColor(150, 155, 165));
        SetColor(ThemeColor.ButtonText, ElementState.Pressed, white);
        SetColor(ThemeColor.ButtonFace, ElementState.Focused, new DuskColor(180, 190, 210));
        SetColor(ThemeColor.ButtonFace, ElementState.Disabled, new DuskColor(200, 200, 200));
        SetColor(ThemeColor.ButtonText, ElementState.Disabled, new DuskColor(140, 140, 140));

        // Window colors
        SetColor(ThemeColor.WindowBackground, bgColor);
        SetColor(ThemeColor.WindowTitle, accentBlue);
        SetColor(ThemeColor.WindowTitleText, white);

        // Input colors
        SetColor(ThemeColor.InputBackground, white);
        SetColor(ThemeColor.InputText, black);
        SetColor(ThemeColor.InputBorder, darkColor);
        SetColor(ThemeColor.InputBackground, ElementState.Focused, new DuskColor(255, 255, 245));
        SetColor(ThemeColor.InputBorder, ElementState.Focused, accentGold);

        // Selection
        SetColor(ThemeColor.SelectionBackground, accentBlue);
        SetColor(ThemeColor.SelectionText, white);

        // States
        SetColor(ThemeColor.DisabledBackground, new DuskColor(210, 210, 210));
        SetColor(ThemeColor.DisabledText, new DuskColor(130, 130, 130));
        SetColor(ThemeColor.ErrorBackground, new DuskColor(255, 210, 210));
        SetColor(ThemeColor.ErrorText, new DuskColor(170, 40, 40));
        SetColor(ThemeColor.SuccessBackground, new DuskColor(210, 250, 210));
        SetColor(ThemeColor.SuccessText, new DuskColor(30, 130, 30));
        SetColor(ThemeColor.WarningBackground, new DuskColor(255, 250, 210));
        SetColor(ThemeColor.WarningText, new DuskColor(170, 130, 20));

        // Royale-style bevels (softer)
        SetBevel(ThemeBevelRole.Button, BevelStyle.RaisedSoft);
        SetBevel(ThemeBevelRole.ButtonPressed, BevelStyle.SunkenSoft);
        SetBevel(ThemeBevelRole.Input, BevelStyle.Sunken);
        SetBevel(ThemeBevelRole.Panel, BevelStyle.Etched);
        SetBevel(ThemeBevelRole.Window, BevelStyle.RaisedSoft);

        // Metrics - slightly larger for modern displays
        SetMetric(ThemeMetric.BevelDepth, 2);
        SetMetric(ThemeMetric.BorderRadius, 2); // Slight rounding
        SetMetric(ThemeMetric.ButtonPadding, 8);
        SetMetric(ThemeMetric.InputPadding, 5);

        // Fonts
        SetFont(ThemeFontRole.Default, new DuskFont("Segoe UI", 9));
        SetFont(ThemeFontRole.Title, new DuskFont("Segoe UI", 12, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Heading, new DuskFont("Segoe UI", 10, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Button, new DuskFont("Segoe UI", 9));
        SetFont(ThemeFontRole.Monospace, new DuskFont("Consolas", 9));
    }

    protected override void ApplyMood(ThemeMood mood)
    {
        base.ApplyMood(mood);

        var profile = MoodProfile.GetProfile(mood);

        if (profile.AccentColor.HasValue)
        {
            SetColor(ThemeColor.Accent, profile.AccentColor.Value);
        }

        var baseBg = new DuskColor(180, 185, 195);
        SetColor(ThemeColor.Background, profile.Apply(baseBg));
        SetColor(ThemeColor.WindowBackground, profile.Apply(baseBg));
    }

    public static AmigaMUIRoyale Create() => new();
}
