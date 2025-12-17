namespace DUSK.DemoApp;

using DUSK.Core;
using DUSK.Engine;
using DUSK.UI;
using DUSK.Theme;
using DUSK.Visuals;

/// <summary>
/// Demo dashboard scene showing more complex UI.
/// Demonstrates layout, visuals, and theming.
/// </summary>
public class DemoDashboardScene : SceneBase
{
    private WaveFXEngine? _fxEngine;
    private UIText? _statusText;
    private UIForm? _mainContent;

    public DemoDashboardScene() : base("dashboard-scene", "DUSK Dashboard")
    {
        BackgroundColor = DuskColor.AmigaGray;
    }

    protected override void OnInitialize()
    {
        Theme = ThemeManager.Instance.CurrentTheme;

        // Initialize visual effects
        _fxEngine = new WaveFXEngine();
        _fxEngine.WaveEngine.AddLayer(WaveStyle.SubtlePulse);
        _fxEngine.Start();

        // Header
        var header = new UIForm
        {
            Bounds = new DuskRect(0, 0, Bounds.Width, 30),
            ShowBorder = false,
            ShowTitle = false,
            BackgroundColor = DuskColor.AmigaBlue
        };

        var headerTitle = new UIText("DUSK Dashboard")
        {
            IsEditable = false,
            Bounds = new DuskRect(10, 5, 200, 20),
            Font = new DuskFont("Topaz", 10, DuskFontStyle.Bold)
        };
        header.AddChild(headerTitle);

        // Navigation panel
        var navPanel = new UIForm("Navigation")
        {
            Bounds = new DuskRect(0, 30, 150, Bounds.Height - 50),
            LayoutMode = LayoutMode.Vertical,
            Spacing = 4
        };

        var navButtons = new[]
        {
            ("Home", "home"),
            ("Settings", "settings"),
            ("Themes", "themes"),
            ("Cache", "cache"),
            ("About", "about")
        };

        foreach (var (text, id) in navButtons)
        {
            var btn = new UIButton(text)
            {
                Name = $"nav-{id}",
                Bounds = new DuskRect(0, 0, 130, 25),
                Style = ButtonStyle.AmigaMUI
            };
            btn.Click += (_, _) => NavigateTo(id);
            navPanel.AddChild(btn);
        }
        navPanel.PerformLayout();

        // Main content area
        _mainContent = new UIForm("Content")
        {
            Bounds = new DuskRect(160, 30, Bounds.Width - 170, Bounds.Height - 50),
            LayoutMode = LayoutMode.Vertical
        };

        // Build welcome content
        BuildWelcomeContent();

        // Status bar
        var statusBar = new UIForm
        {
            Bounds = new DuskRect(0, Bounds.Height - 20, Bounds.Width, 20),
            ShowBorder = false,
            ShowTitle = false,
            BackgroundColor = new DuskColor(150, 150, 150)
        };

        _statusText = new UIText("Ready")
        {
            IsEditable = false,
            Bounds = new DuskRect(5, 2, Bounds.Width - 10, 16)
        };
        statusBar.AddChild(_statusText);

        // Add all components
        AddElement(header);
        AddElement(navPanel);
        AddElement(_mainContent);
        AddElement(statusBar);
    }

    private void BuildWelcomeContent()
    {
        if (_mainContent == null) return;

        var welcomeText = new UIText("Welcome to DUSK Framework!")
        {
            IsEditable = false,
            Font = new DuskFont("Topaz", 11, DuskFontStyle.Bold),
            Bounds = new DuskRect(10, 10, 400, 25)
        };

        var descText = new UIText(
            "DUSK is a modern UI framework inspired by Amiga MUI,\n" +
            "designed as a drop-in replacement for Windows Forms.\n\n" +
            "Features:\n" +
            "- Amiga MUI style theming\n" +
            "- Scene-based architecture (like Unity)\n" +
            "- 3-layer cache (Memory/Redis/MongoDB)\n" +
            "- AI-friendly migration tools\n" +
            "- Visual effects engine")
        {
            IsEditable = false,
            Bounds = new DuskRect(10, 40, 400, 150),
            IsMultiLine = true
        };

        var themeSection = new UIForm("Theme Preview")
        {
            Bounds = new DuskRect(10, 200, 300, 120),
            LayoutMode = LayoutMode.Vertical,
            Spacing = 8
        };

        var themes = new[] { "Amiga MUI Classic", "Amiga MUI Royale", "Modern Flat", "Classic Win32" };
        foreach (var themeName in themes)
        {
            var btn = new UIButton(themeName)
            {
                Bounds = new DuskRect(0, 0, 280, 24)
            };
            btn.Click += (_, _) => SwitchTheme(themeName);
            themeSection.AddChild(btn);
        }
        themeSection.PerformLayout();

        _mainContent.AddChild(welcomeText);
        _mainContent.AddChild(descText);
        _mainContent.AddChild(themeSection);
    }

    private void NavigateTo(string section)
    {
        if (_statusText != null)
        {
            _statusText.Text = $"Navigated to: {section}";
        }
    }

    private void SwitchTheme(string themeName)
    {
        ThemeManager.Instance.SetTheme(themeName);
        Theme = ThemeManager.Instance.CurrentTheme;

        if (_statusText != null)
        {
            _statusText.Text = $"Theme changed to: {themeName}";
        }

        // Refresh all elements with new theme
        foreach (var element in Elements)
        {
            element.Theme = Theme;
        }
    }

    protected override void OnUpdate(float deltaTime)
    {
        _fxEngine?.Update(deltaTime);
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Could add visual effects to background here
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fxEngine?.Dispose();
        }
        base.Dispose(disposing);
    }
}
