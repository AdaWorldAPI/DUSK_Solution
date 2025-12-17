namespace DUSK.Core;

/// <summary>
/// Defines theming capabilities for DUSK UI elements.
/// Inspired by Amiga MUI's theming system with mood-based variations.
/// </summary>
public interface ITheme
{
    string Name { get; }
    ThemeStyle Style { get; }

    DuskColor GetColor(ThemeColor colorType);
    DuskColor GetColor(ThemeColor colorType, ElementState state);

    DuskFont GetFont(ThemeFontRole role);
    int GetMetric(ThemeMetric metric);
    BevelStyle GetBevelStyle(ThemeBevelRole role);

    ITheme WithMood(ThemeMood mood);
}

public enum ThemeStyle
{
    AmigaMUI,
    AmigaRoyale,
    ModernFlat,
    ClassicWin32,
    Custom
}

public enum ThemeColor
{
    Background,
    Foreground,
    Primary,
    Secondary,
    Accent,
    Border,
    BorderLight,
    BorderDark,
    ButtonFace,
    ButtonText,
    WindowBackground,
    WindowTitle,
    WindowTitleText,
    InputBackground,
    InputText,
    InputBorder,
    SelectionBackground,
    SelectionText,
    DisabledBackground,
    DisabledText,
    ErrorBackground,
    ErrorText,
    SuccessBackground,
    SuccessText,
    WarningBackground,
    WarningText
}

public enum ElementState
{
    Normal,
    Hover,
    Pressed,
    Focused,
    Disabled,
    Selected
}

public enum ThemeFontRole
{
    Default,
    Title,
    Heading,
    Body,
    Caption,
    Button,
    Input,
    Monospace
}

public enum ThemeMetric
{
    BorderWidth,
    BorderRadius,
    ButtonPadding,
    InputPadding,
    ElementSpacing,
    WindowPadding,
    BevelDepth,
    FocusWidth,
    ScrollbarWidth,
    MinButtonWidth,
    MinButtonHeight
}

public enum ThemeBevelRole
{
    Button,
    ButtonPressed,
    Input,
    Panel,
    Window,
    GroupBox,
    Divider
}

public enum ThemeMood
{
    Neutral,
    Calm,
    Energetic,
    Focused,
    Warning,
    Error,
    Success
}

/// <summary>
/// Provides theme instances to the framework.
/// </summary>
public interface IThemeProvider
{
    ITheme CurrentTheme { get; }
    IReadOnlyList<ITheme> AvailableThemes { get; }

    void SetTheme(string themeName);
    void SetTheme(ITheme theme);

    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

public class ThemeChangedEventArgs : EventArgs
{
    public ITheme OldTheme { get; }
    public ITheme NewTheme { get; }

    public ThemeChangedEventArgs(ITheme oldTheme, ITheme newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
    }
}
