namespace DUSK.Engine;

using DUSK.Core;

/// <summary>
/// Base implementation of IScene providing common functionality.
/// Equivalent to Form in WinForms, provides the container for UI elements.
/// </summary>
public abstract class SceneBase : IScene
{
    private readonly List<IUIElement> _elements = new();
    private SceneState _state = SceneState.Created;
    private bool _disposed;

    public string Id { get; }
    public string Title { get; set; }
    public SceneState State => _state;
    public IScene? ParentScene { get; set; }
    public IReadOnlyList<IUIElement> Elements => _elements.AsReadOnly();

    public DuskRect Bounds { get; set; } = new(0, 0, 800, 600);
    public DuskColor BackgroundColor { get; set; } = DuskColor.AmigaGray;
    public ITheme? Theme { get; set; }

    public event EventHandler<SceneEventArgs>? Loading;
    public event EventHandler<SceneEventArgs>? Loaded;
    public event EventHandler<SceneEventArgs>? Activating;
    public event EventHandler<SceneEventArgs>? Activated;
    public event EventHandler<SceneEventArgs>? Deactivating;
    public event EventHandler<SceneEventArgs>? Deactivated;
    public event EventHandler<SceneEventArgs>? Closing;
    public event EventHandler<SceneEventArgs>? Closed;

    protected SceneBase(string? id = null, string? title = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        Title = title ?? GetType().Name;
    }

    public virtual void AddElement(IUIElement element)
    {
        if (!_elements.Contains(element))
        {
            _elements.Add(element);
            element.Parent = null; // Scene is root, not a UIElement
        }
    }

    public virtual void RemoveElement(IUIElement element)
    {
        _elements.Remove(element);
        element.Parent = null;
    }

    public IUIElement? FindElement(string id)
    {
        return FindElementRecursive(_elements, id);
    }

    public T? FindElement<T>(string id) where T : class, IUIElement
    {
        return FindElement(id) as T;
    }

    private static IUIElement? FindElementRecursive(IEnumerable<IUIElement> elements, string id)
    {
        foreach (var element in elements)
        {
            if (element.Id == id) return element;
            var found = FindElementRecursive(element.Children, id);
            if (found != null) return found;
        }
        return null;
    }

    public virtual void Initialize()
    {
        _state = SceneState.Loading;
        var args = new SceneEventArgs(this);
        Loading?.Invoke(this, args);

        OnInitialize();

        _state = SceneState.Loaded;
        Loaded?.Invoke(this, args);
    }

    protected virtual void OnInitialize() { }

    public virtual void Update(float deltaTime)
    {
        if (_state != SceneState.Active) return;

        OnUpdate(deltaTime);

        foreach (var element in _elements)
        {
            if (element.Visible)
            {
                element.Update(deltaTime);
            }
        }
    }

    protected virtual void OnUpdate(float deltaTime) { }

    public virtual void Render(IRenderer renderer)
    {
        if (_state == SceneState.Closed) return;

        // Draw background
        var bgColor = Theme?.GetColor(ThemeColor.WindowBackground) ?? BackgroundColor;
        renderer.Clear(bgColor);

        OnRender(renderer);

        // Render all elements back to front
        foreach (var element in _elements)
        {
            if (element.Visible)
            {
                element.Render(renderer);
            }
        }
    }

    protected virtual void OnRender(IRenderer renderer) { }

    public virtual void Show()
    {
        var args = new SceneEventArgs(this);
        Activating?.Invoke(this, args);
        if (args.Cancel) return;

        _state = SceneState.Active;
        Activated?.Invoke(this, args);

        OnShow();
    }

    protected virtual void OnShow() { }

    public virtual void Hide()
    {
        var args = new SceneEventArgs(this);
        Deactivating?.Invoke(this, args);
        if (args.Cancel) return;

        _state = SceneState.Inactive;
        Deactivated?.Invoke(this, args);

        OnHide();
    }

    protected virtual void OnHide() { }

    public virtual void Close()
    {
        var args = new SceneEventArgs(this);
        Closing?.Invoke(this, args);
        if (args.Cancel) return;

        _state = SceneState.Closing;
        OnClose();

        _state = SceneState.Closed;
        Closed?.Invoke(this, args);
    }

    protected virtual void OnClose() { }

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
            foreach (var element in _elements)
            {
                (element as IDisposable)?.Dispose();
            }
            _elements.Clear();
        }

        _disposed = true;
    }
}
