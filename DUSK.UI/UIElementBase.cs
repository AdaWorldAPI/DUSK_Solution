namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Base class for all UI elements in DUSK.
/// Provides common functionality for positioning, visibility, events, and theming.
/// </summary>
public abstract class UIElementBase : IUIElement, IDisposable
{
    private readonly List<IUIElement> _children = new();
    private bool _disposed;
    private bool _isFocused;
    private bool _isHovered;
    private bool _isPressed;

    public string Id { get; }
    public string Name { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public DuskRect Bounds { get; set; }
    public IUIElement? Parent { get; set; }
    public IReadOnlyList<IUIElement> Children => _children.AsReadOnly();
    public object? Tag { get; set; }
    public ITheme? Theme { get; set; }

    public bool IsFocused => _isFocused;
    public bool IsHovered => _isHovered;
    public bool IsPressed => _isPressed;

    // Padding and margins
    public int PaddingLeft { get; set; } = 4;
    public int PaddingRight { get; set; } = 4;
    public int PaddingTop { get; set; } = 2;
    public int PaddingBottom { get; set; } = 2;
    public int MarginLeft { get; set; }
    public int MarginRight { get; set; }
    public int MarginTop { get; set; }
    public int MarginBottom { get; set; }

    // Events
    public event EventHandler<MouseEventArgs>? MouseEnter;
    public event EventHandler<MouseEventArgs>? MouseLeave;
    public event EventHandler<MouseEventArgs>? MouseDown;
    public event EventHandler<MouseEventArgs>? MouseUp;
    public event EventHandler<MouseEventArgs>? Click;
    public event EventHandler<KeyEventArgs>? KeyDown;
    public event EventHandler<KeyEventArgs>? KeyUp;
    public event EventHandler? GotFocus;
    public event EventHandler? LostFocus;
    public event EventHandler? EnabledChanged;
    public event EventHandler? VisibleChanged;

    protected UIElementBase(string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        Name = Id;
    }

    public virtual void AddChild(IUIElement child)
    {
        if (!_children.Contains(child))
        {
            _children.Add(child);
            child.Parent = this;
        }
    }

    public virtual void RemoveChild(IUIElement child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
        }
    }

    public virtual void Update(float deltaTime)
    {
        if (!Visible || !Enabled) return;

        OnUpdate(deltaTime);

        foreach (var child in _children)
        {
            child.Update(deltaTime);
        }
    }

    protected virtual void OnUpdate(float deltaTime) { }

    public virtual void Render(IRenderer renderer)
    {
        if (!Visible) return;

        OnRender(renderer);

        foreach (var child in _children)
        {
            if (child.Visible)
            {
                child.Render(renderer);
            }
        }
    }

    protected abstract void OnRender(IRenderer renderer);

    public virtual bool HitTest(DuskPoint point)
    {
        return Bounds.Contains(point);
    }

    public virtual void Focus()
    {
        if (_isFocused) return;
        _isFocused = true;
        GotFocus?.Invoke(this, EventArgs.Empty);
    }

    public virtual void Blur()
    {
        if (!_isFocused) return;
        _isFocused = false;
        LostFocus?.Invoke(this, EventArgs.Empty);
    }

    public virtual void HandleMouseEnter(MouseEventArgs args)
    {
        if (!Enabled) return;
        _isHovered = true;
        MouseEnter?.Invoke(this, args);
    }

    public virtual void HandleMouseLeave(MouseEventArgs args)
    {
        _isHovered = false;
        _isPressed = false;
        MouseLeave?.Invoke(this, args);
    }

    public virtual void HandleMouseDown(MouseEventArgs args)
    {
        if (!Enabled) return;
        _isPressed = true;
        MouseDown?.Invoke(this, args);
    }

    public virtual void HandleMouseUp(MouseEventArgs args)
    {
        if (!Enabled) return;
        var wasPressed = _isPressed;
        _isPressed = false;
        MouseUp?.Invoke(this, args);

        if (wasPressed && _isHovered)
        {
            Click?.Invoke(this, args);
        }
    }

    public virtual void HandleKeyDown(KeyEventArgs args)
    {
        if (!Enabled) return;
        KeyDown?.Invoke(this, args);
    }

    public virtual void HandleKeyUp(KeyEventArgs args)
    {
        if (!Enabled) return;
        KeyUp?.Invoke(this, args);
    }

    protected ElementState GetCurrentState()
    {
        if (!Enabled) return ElementState.Disabled;
        if (_isPressed) return ElementState.Pressed;
        if (_isFocused) return ElementState.Focused;
        if (_isHovered) return ElementState.Hover;
        return ElementState.Normal;
    }

    protected DuskColor GetThemedColor(ThemeColor colorType)
    {
        var theme = Theme ?? GetInheritedTheme();
        return theme?.GetColor(colorType, GetCurrentState()) ?? DuskColor.Black;
    }

    protected ITheme? GetInheritedTheme()
    {
        var current = Parent;
        while (current != null)
        {
            if (current.Theme != null) return current.Theme;
            current = current.Parent;
        }
        return null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            foreach (var child in _children)
            {
                (child as IDisposable)?.Dispose();
            }
            _children.Clear();
        }

        _disposed = true;
    }
}
