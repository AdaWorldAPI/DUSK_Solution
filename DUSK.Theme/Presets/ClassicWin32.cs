namespace DUSK.Theme.Presets;

using DUSK.Core;

/// <summary>
/// Classic Windows 95/98/2000 theme.
/// For applications targeting familiar Win32 aesthetics.
/// </summary>
public class ClassicWin32 : ThemeProfile
{
    private ClassicWin32() : base("Classic Win32", ThemeStyle.ClassicWin32)
    {
    }

    protected override void InitializeDefaults()
    {
        base.InitializeDefaults();

        // Classic Windows colors
        var buttonFace = new DuskColor(212, 208, 200);   // Classic Win32 button gray
        var buttonHighlight = new DuskColor(255, 255, 255);
        var buttonShadow = new DuskColor(128, 128, 128);
        var buttonDkShadow = new DuskColor(64, 64, 64);
        var windowBg = buttonFace;
        var windowText = DuskColor.Black;
        var activeCaption = new DuskColor(0, 0, 128);    // Navy blue
        var captionText = DuskColor.White;
        var highlight = new DuskColor(0, 0, 128);
        var highlightText = DuskColor.White;

        // Base colors
        SetColor(ThemeColor.Background, windowBg);
        SetColor(ThemeColor.Foreground, windowText);
        SetColor(ThemeColor.Primary, activeCaption);
        SetColor(ThemeColor.Secondary, buttonShadow);
        SetColor(ThemeColor.Accent, new DuskColor(0, 128, 0)); // Green
        SetColor(ThemeColor.Border, buttonDkShadow);
        SetColor(ThemeColor.BorderLight, buttonHighlight);
        SetColor(ThemeColor.BorderDark, buttonDkShadow);

        // Button colors
        SetColor(ThemeColor.ButtonFace, buttonFace);
        SetColor(ThemeColor.ButtonText, windowText);
        SetColor(ThemeColor.ButtonFace, ElementState.Hover, buttonFace);
        SetColor(ThemeColor.ButtonFace, ElementState.Pressed, buttonFace);
        SetColor(ThemeColor.ButtonFace, ElementState.Focused, buttonFace);
        SetColor(ThemeColor.ButtonFace, ElementState.Disabled, buttonFace);
        SetColor(ThemeColor.ButtonText, ElementState.Disabled, buttonShadow);

        // Window colors
        SetColor(ThemeColor.WindowBackground, windowBg);
        SetColor(ThemeColor.WindowTitle, activeCaption);
        SetColor(ThemeColor.WindowTitleText, captionText);

        // Input colors
        SetColor(ThemeColor.InputBackground, DuskColor.White);
        SetColor(ThemeColor.InputText, windowText);
        SetColor(ThemeColor.InputBorder, buttonShadow);
        SetColor(ThemeColor.InputBackground, ElementState.Focused, DuskColor.White);
        SetColor(ThemeColor.InputBorder, ElementState.Focused, buttonDkShadow);

        // Selection
        SetColor(ThemeColor.SelectionBackground, highlight);
        SetColor(ThemeColor.SelectionText, highlightText);

        // States
        SetColor(ThemeColor.DisabledBackground, buttonFace);
        SetColor(ThemeColor.DisabledText, buttonShadow);
        SetColor(ThemeColor.ErrorBackground, new DuskColor(255, 192, 192));
        SetColor(ThemeColor.ErrorText, new DuskColor(192, 0, 0));
        SetColor(ThemeColor.SuccessBackground, new DuskColor(192, 255, 192));
        SetColor(ThemeColor.SuccessText, new DuskColor(0, 128, 0));
        SetColor(ThemeColor.WarningBackground, new DuskColor(255, 255, 192));
        SetColor(ThemeColor.WarningText, new DuskColor(128, 128, 0));

        // Classic 3D bevels
        SetBevel(ThemeBevelRole.Button, BevelStyle.Raised);
        SetBevel(ThemeBevelRole.ButtonPressed, BevelStyle.Sunken);
        SetBevel(ThemeBevelRole.Input, BevelStyle.Sunken);
        SetBevel(ThemeBevelRole.Panel, BevelStyle.Etched);
        SetBevel(ThemeBevelRole.Window, BevelStyle.Raised);
        SetBevel(ThemeBevelRole.GroupBox, BevelStyle.Etched);

        // Classic metrics
        SetMetric(ThemeMetric.BevelDepth, 2);
        SetMetric(ThemeMetric.BorderRadius, 0);
        SetMetric(ThemeMetric.BorderWidth, 1);
        SetMetric(ThemeMetric.ButtonPadding, 6);
        SetMetric(ThemeMetric.InputPadding, 3);
        SetMetric(ThemeMetric.MinButtonWidth, 75);
        SetMetric(ThemeMetric.MinButtonHeight, 23);

        // Classic Windows fonts
        SetFont(ThemeFontRole.Default, new DuskFont("MS Sans Serif", 8));
        SetFont(ThemeFontRole.Title, new DuskFont("MS Sans Serif", 8, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Heading, new DuskFont("MS Sans Serif", 8, DuskFontStyle.Bold));
        SetFont(ThemeFontRole.Button, new DuskFont("MS Sans Serif", 8));
        SetFont(ThemeFontRole.Monospace, new DuskFont("Courier New", 10));
    }

    public static ClassicWin32 Create() => new();
}
