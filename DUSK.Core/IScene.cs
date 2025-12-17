namespace DUSK.Core;

/// <summary>
/// Represents a scene in the DUSK framework (equivalent to a Form/Window in WinForms).
/// Scenes are the primary containers for UI elements and manage their lifecycle.
/// </summary>
public interface IScene : IDisposable
{
    string Id { get; }
    string Title { get; set; }
    SceneState State { get; }
    IScene? ParentScene { get; }
    IReadOnlyList<IUIElement> Elements { get; }

    event EventHandler<SceneEventArgs>? Loading;
    event EventHandler<SceneEventArgs>? Loaded;
    event EventHandler<SceneEventArgs>? Activating;
    event EventHandler<SceneEventArgs>? Activated;
    event EventHandler<SceneEventArgs>? Deactivating;
    event EventHandler<SceneEventArgs>? Deactivated;
    event EventHandler<SceneEventArgs>? Closing;
    event EventHandler<SceneEventArgs>? Closed;

    void AddElement(IUIElement element);
    void RemoveElement(IUIElement element);
    IUIElement? FindElement(string id);
    T? FindElement<T>(string id) where T : class, IUIElement;

    void Initialize();
    void Update(float deltaTime);
    void Render(IRenderer renderer);

    void Show();
    void Hide();
    void Close();
}

public enum SceneState
{
    Created,
    Loading,
    Loaded,
    Active,
    Inactive,
    Closing,
    Closed
}

public class SceneEventArgs : EventArgs
{
    public IScene Scene { get; }
    public bool Cancel { get; set; }

    public SceneEventArgs(IScene scene)
    {
        Scene = scene;
    }
}

/// <summary>
/// Represents a UI element within a scene.
/// </summary>
public interface IUIElement
{
    string Id { get; }
    string Name { get; set; }
    bool Visible { get; set; }
    bool Enabled { get; set; }
    DuskRect Bounds { get; set; }
    IUIElement? Parent { get; set; }
    IReadOnlyList<IUIElement> Children { get; }

    object? Tag { get; set; }
    ITheme? Theme { get; set; }

    event EventHandler<MouseEventArgs>? MouseEnter;
    event EventHandler<MouseEventArgs>? MouseLeave;
    event EventHandler<MouseEventArgs>? MouseDown;
    event EventHandler<MouseEventArgs>? MouseUp;
    event EventHandler<MouseEventArgs>? Click;
    event EventHandler<KeyEventArgs>? KeyDown;
    event EventHandler<KeyEventArgs>? KeyUp;
    event EventHandler? GotFocus;
    event EventHandler? LostFocus;

    void AddChild(IUIElement child);
    void RemoveChild(IUIElement child);

    void Update(float deltaTime);
    void Render(IRenderer renderer);
    bool HitTest(DuskPoint point);
    void Focus();
    void Blur();
}

public class MouseEventArgs : EventArgs
{
    public DuskPoint Position { get; }
    public MouseButton Button { get; }
    public int Clicks { get; }

    public MouseEventArgs(DuskPoint position, MouseButton button = MouseButton.None, int clicks = 0)
    {
        Position = position;
        Button = button;
        Clicks = clicks;
    }
}

public class KeyEventArgs : EventArgs
{
    public DuskKey Key { get; }
    public bool Shift { get; }
    public bool Control { get; }
    public bool Alt { get; }
    public bool Handled { get; set; }

    public KeyEventArgs(DuskKey key, bool shift = false, bool control = false, bool alt = false)
    {
        Key = key;
        Shift = shift;
        Control = control;
        Alt = alt;
    }
}

public enum MouseButton
{
    None,
    Left,
    Right,
    Middle
}

public enum DuskKey
{
    None,
    Enter, Escape, Tab, Backspace, Delete,
    Left, Right, Up, Down,
    Home, End, PageUp, PageDown,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Space, Comma, Period, Semicolon, Quote, Slash, Backslash,
    LeftBracket, RightBracket, Minus, Equals, Grave
}
