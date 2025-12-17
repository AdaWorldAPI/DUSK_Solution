namespace DUSK.Theme;

using DUSK.Core;

/// <summary>
/// Implementation of IThemeProvider that manages available themes.
/// </summary>
public class ThemeProvider : IThemeProvider
{
    private readonly List<ITheme> _themes = new();
    private ITheme _currentTheme;

    public ITheme CurrentTheme => _currentTheme;
    public IReadOnlyList<ITheme> AvailableThemes => _themes.AsReadOnly();

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeProvider(ITheme defaultTheme)
    {
        _currentTheme = defaultTheme;
        _themes.Add(defaultTheme);
    }

    public void RegisterTheme(ITheme theme)
    {
        if (_themes.All(t => t.Name != theme.Name))
        {
            _themes.Add(theme);
        }
    }

    public void SetTheme(string themeName)
    {
        var theme = _themes.FirstOrDefault(t => t.Name == themeName);
        if (theme != null)
        {
            SetTheme(theme);
        }
    }

    public void SetTheme(ITheme theme)
    {
        if (_currentTheme == theme) return;

        var oldTheme = _currentTheme;
        _currentTheme = theme;

        if (!_themes.Contains(theme))
        {
            _themes.Add(theme);
        }

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
    }
}
