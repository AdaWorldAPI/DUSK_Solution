namespace DUSK.Theme;

using DUSK.Core;
using DUSK.Theme.Presets;

/// <summary>
/// Central theme management for the DUSK framework.
/// Provides access to built-in themes and theme switching.
/// </summary>
public sealed class ThemeManager : IThemeProvider, IDisposable
{
    private static ThemeManager? _instance;
    private readonly ThemeProvider _provider;
    private readonly BreathCore _breathCore;
    private MoodProfile _currentMood = MoodProfile.Neutral;
    private bool _disposed;

    public static ThemeManager Instance => _instance ??= new ThemeManager();

    public ITheme CurrentTheme => _provider.CurrentTheme;
    public IReadOnlyList<ITheme> AvailableThemes => _provider.AvailableThemes;
    public BreathCore BreathCore => _breathCore;
    public MoodProfile CurrentMood => _currentMood;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    private ThemeManager()
    {
        _breathCore = new BreathCore();

        // Initialize with Amiga MUI Classic as default
        var defaultTheme = AmigaMUIClassic.Create();
        _provider = new ThemeProvider(defaultTheme);

        // Register built-in themes
        _provider.RegisterTheme(AmigaMUIRoyale.Create());
        _provider.RegisterTheme(ModernFlat.Create());
        _provider.RegisterTheme(ClassicWin32.Create());

        _provider.ThemeChanged += (s, e) => ThemeChanged?.Invoke(this, e);

        _breathCore.Start();
    }

    public void SetTheme(string themeName)
    {
        _provider.SetTheme(themeName);
    }

    public void SetTheme(ITheme theme)
    {
        _provider.SetTheme(theme);
    }

    public void RegisterTheme(ITheme theme)
    {
        _provider.RegisterTheme(theme);
    }

    public void SetMood(ThemeMood mood)
    {
        _currentMood = MoodProfile.GetProfile(mood);
    }

    public void SetMood(MoodProfile mood)
    {
        _currentMood = mood;
    }

    public ITheme GetThemedWithMood()
    {
        return CurrentTheme.WithMood(_currentMood.Mood);
    }

    public DuskColor ApplyMoodAndBreath(DuskColor color)
    {
        var moodedColor = _currentMood.Apply(color);
        return _breathCore.ApplyBreath(moodedColor);
    }

    public void Update(float deltaTime)
    {
        _breathCore.Update(deltaTime);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _breathCore.Dispose();
    }
}
