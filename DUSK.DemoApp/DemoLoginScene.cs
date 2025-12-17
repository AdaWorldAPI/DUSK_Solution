namespace DUSK.DemoApp;

using DUSK.Core;
using DUSK.Engine;
using DUSK.UI;
using DUSK.Theme;

/// <summary>
/// Demo login scene showcasing DUSK UI components.
/// Classic Amiga MUI style login form.
/// </summary>
public class DemoLoginScene : SceneBase
{
    private UIText? _usernameInput;
    private UIText? _passwordInput;
    private UIButton? _loginButton;
    private UIButton? _exitButton;
    private UIText? _statusLabel;

    public DemoLoginScene() : base("login-scene", "DUSK Login Demo")
    {
        BackgroundColor = DuskColor.AmigaGray;
    }

    protected override void OnInitialize()
    {
        Theme = ThemeManager.Instance.CurrentTheme;

        // Create centered login form
        var formWidth = 300;
        var formHeight = 200;
        var formX = (Bounds.Width - formWidth) / 2;
        var formY = (Bounds.Height - formHeight) / 2;

        var loginForm = new UIForm("Login")
        {
            Bounds = new DuskRect(formX, formY, formWidth, formHeight),
            LayoutMode = LayoutMode.Vertical,
            Spacing = 8,
            Theme = Theme
        };

        // Username row
        var usernameLabel = new UIText("Username:")
        {
            IsEditable = false,
            Bounds = new DuskRect(0, 0, 80, 20)
        };

        _usernameInput = new UIText
        {
            IsEditable = true,
            PlaceholderText = "Enter username",
            Bounds = new DuskRect(0, 0, 200, 24)
        };

        // Password row
        var passwordLabel = new UIText("Password:")
        {
            IsEditable = false,
            Bounds = new DuskRect(0, 0, 80, 20)
        };

        _passwordInput = new UIText
        {
            IsEditable = true,
            IsPassword = true,
            PlaceholderText = "Enter password",
            Bounds = new DuskRect(0, 0, 200, 24)
        };

        // Buttons
        var buttonRow = new UIForm
        {
            LayoutMode = LayoutMode.Horizontal,
            ShowBorder = false,
            ShowTitle = false,
            Bounds = new DuskRect(0, 0, 280, 30),
            Spacing = 8
        };

        _loginButton = new UIButton("Login")
        {
            Bounds = new DuskRect(0, 0, 100, 28),
            Style = ButtonStyle.AmigaMUI
        };
        _loginButton.Click += OnLoginClicked;

        _exitButton = new UIButton("Exit")
        {
            Bounds = new DuskRect(0, 0, 100, 28),
            Style = ButtonStyle.AmigaMUI
        };
        _exitButton.Click += OnExitClicked;

        buttonRow.AddChild(_loginButton);
        buttonRow.AddChild(_exitButton);

        // Status
        _statusLabel = new UIText
        {
            IsEditable = false,
            Text = "Welcome to DUSK Framework Demo",
            HorizontalAlignment = TextAlignment.Center,
            Bounds = new DuskRect(0, 0, 280, 20)
        };

        // Assemble form
        loginForm.AddChild(usernameLabel);
        loginForm.AddChild(_usernameInput);
        loginForm.AddChild(passwordLabel);
        loginForm.AddChild(_passwordInput);
        loginForm.AddChild(buttonRow);
        loginForm.AddChild(_statusLabel);

        loginForm.PerformLayout();
        AddElement(loginForm);

        // Add title at top
        var titleLabel = new UIText("DUSK Framework")
        {
            IsEditable = false,
            Bounds = new DuskRect(0, 2, Bounds.Width, 20),
            HorizontalAlignment = TextAlignment.Center,
            Font = new DuskFont("Topaz", 12, DuskFontStyle.Bold)
        };
        AddElement(titleLabel);
    }

    private void OnLoginClicked(object? sender, MouseEventArgs e)
    {
        var username = _usernameInput?.Text ?? string.Empty;
        var password = _passwordInput?.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username))
        {
            if (_statusLabel != null)
                _statusLabel.Text = "Please enter a username";
            return;
        }

        // Demo: accept any login
        if (_statusLabel != null)
            _statusLabel.Text = $"Welcome, {username}!";

        // Navigate to dashboard after delay
        Task.Delay(500).ContinueWith(_ =>
        {
            // Would navigate to dashboard here
        });
    }

    private void OnExitClicked(object? sender, MouseEventArgs e)
    {
        Close();
    }
}
