namespace DUSK.Core.Input;

/// <summary>
/// Central input manager for handling keyboard and mouse events.
/// Provides unified input handling across different runtime adapters.
/// </summary>
public sealed class InputManager : IDisposable
{
    private static InputManager? _instance;
    private static readonly object Lock = new();

    private readonly KeyboardState _keyboardState = new();
    private readonly MouseState _mouseState = new();
    private readonly List<IInputHandler> _handlers = new();
    private bool _disposed;

    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    _instance ??= new InputManager();
                }
            }
            return _instance;
        }
    }

    public KeyboardState Keyboard => _keyboardState;
    public MouseState Mouse => _mouseState;

    /// <summary>
    /// Gets the element currently under the mouse cursor.
    /// </summary>
    public IUIElement? HoveredElement { get; private set; }

    /// <summary>
    /// Gets the element that currently has keyboard focus.
    /// </summary>
    public IUIElement? FocusedElement { get; private set; }

    /// <summary>
    /// Gets the element being dragged.
    /// </summary>
    public IUIElement? DraggedElement { get; private set; }

    public event EventHandler<KeyEventArgs>? GlobalKeyDown;
    public event EventHandler<KeyEventArgs>? GlobalKeyUp;
    public event EventHandler<MouseEventArgs>? GlobalMouseDown;
    public event EventHandler<MouseEventArgs>? GlobalMouseUp;
    public event EventHandler<MouseEventArgs>? GlobalMouseMove;
    public event EventHandler<MouseWheelEventArgs>? GlobalMouseWheel;

    private InputManager() { }

    /// <summary>
    /// Register an input handler to receive events.
    /// </summary>
    public void RegisterHandler(IInputHandler handler)
    {
        if (!_handlers.Contains(handler))
        {
            _handlers.Add(handler);
        }
    }

    /// <summary>
    /// Unregister an input handler.
    /// </summary>
    public void UnregisterHandler(IInputHandler handler)
    {
        _handlers.Remove(handler);
    }

    /// <summary>
    /// Process a key down event from the runtime adapter.
    /// </summary>
    public void ProcessKeyDown(DuskKey key, KeyModifiers modifiers)
    {
        _keyboardState.SetKeyDown(key);
        _keyboardState.Modifiers = modifiers;

        var args = new KeyEventArgs(key, modifiers);
        GlobalKeyDown?.Invoke(this, args);

        if (!args.Handled)
        {
            FocusedElement?.HandleKeyDown(args);
        }

        foreach (var handler in _handlers)
        {
            if (args.Handled) break;
            handler.HandleKeyDown(args);
        }
    }

    /// <summary>
    /// Process a key up event from the runtime adapter.
    /// </summary>
    public void ProcessKeyUp(DuskKey key, KeyModifiers modifiers)
    {
        _keyboardState.SetKeyUp(key);
        _keyboardState.Modifiers = modifiers;

        var args = new KeyEventArgs(key, modifiers);
        GlobalKeyUp?.Invoke(this, args);

        if (!args.Handled)
        {
            FocusedElement?.HandleKeyUp(args);
        }

        foreach (var handler in _handlers)
        {
            if (args.Handled) break;
            handler.HandleKeyUp(args);
        }
    }

    /// <summary>
    /// Process a mouse move event from the runtime adapter.
    /// </summary>
    public void ProcessMouseMove(DuskPoint position, IScene? scene)
    {
        var previousPosition = _mouseState.Position;
        _mouseState.Position = position;

        var args = new MouseEventArgs(position, MouseButton.None, 0, _keyboardState.Modifiers);
        GlobalMouseMove?.Invoke(this, args);

        // Handle drag
        if (DraggedElement != null && _mouseState.IsButtonDown(MouseButton.Left))
        {
            var dragArgs = new DragEventArgs(DraggedElement, previousPosition, position);
            (DraggedElement as IDraggable)?.OnDrag(dragArgs);
        }

        // Update hover state
        if (scene != null)
        {
            var newHovered = HitTestScene(scene, position);
            if (newHovered != HoveredElement)
            {
                HoveredElement?.HandleMouseLeave(args);
                HoveredElement = newHovered;
                HoveredElement?.HandleMouseEnter(args);
            }
        }
    }

    /// <summary>
    /// Process a mouse button down event from the runtime adapter.
    /// </summary>
    public void ProcessMouseDown(DuskPoint position, MouseButton button, int clicks, IScene? scene)
    {
        _mouseState.Position = position;
        _mouseState.SetButtonDown(button);

        var args = new MouseEventArgs(position, button, clicks, _keyboardState.Modifiers);
        GlobalMouseDown?.Invoke(this, args);

        if (scene != null && !args.Handled)
        {
            var hitElement = HitTestScene(scene, position);
            if (hitElement != null)
            {
                // Update focus
                if (hitElement != FocusedElement)
                {
                    FocusedElement?.Blur();
                    FocusedElement = hitElement;
                    FocusedElement.Focus();
                }

                hitElement.HandleMouseDown(args);

                // Start drag if draggable
                if (hitElement is IDraggable draggable && draggable.CanDrag)
                {
                    DraggedElement = hitElement;
                    draggable.OnDragStart(new DragEventArgs(hitElement, position, position));
                }
            }
            else
            {
                // Clicked on empty space - clear focus
                FocusedElement?.Blur();
                FocusedElement = null;
            }
        }

        foreach (var handler in _handlers)
        {
            if (args.Handled) break;
            handler.HandleMouseDown(args);
        }
    }

    /// <summary>
    /// Process a mouse button up event from the runtime adapter.
    /// </summary>
    public void ProcessMouseUp(DuskPoint position, MouseButton button, IScene? scene)
    {
        _mouseState.Position = position;
        _mouseState.SetButtonUp(button);

        var args = new MouseEventArgs(position, button, 0, _keyboardState.Modifiers);
        GlobalMouseUp?.Invoke(this, args);

        // End drag
        if (DraggedElement != null)
        {
            (DraggedElement as IDraggable)?.OnDragEnd(new DragEventArgs(DraggedElement, _mouseState.Position, position));
            DraggedElement = null;
        }

        if (scene != null && !args.Handled)
        {
            var hitElement = HitTestScene(scene, position);
            hitElement?.HandleMouseUp(args);
        }

        foreach (var handler in _handlers)
        {
            if (args.Handled) break;
            handler.HandleMouseUp(args);
        }
    }

    /// <summary>
    /// Process a mouse wheel event from the runtime adapter.
    /// </summary>
    public void ProcessMouseWheel(DuskPoint position, int delta)
    {
        var args = new MouseWheelEventArgs(position, delta, _keyboardState.Modifiers);
        GlobalMouseWheel?.Invoke(this, args);

        (HoveredElement as IScrollable)?.OnScroll(delta);

        foreach (var handler in _handlers)
        {
            if (args.Handled) break;
            handler.HandleMouseWheel(args);
        }
    }

    /// <summary>
    /// Process text input (character typed).
    /// </summary>
    public void ProcessTextInput(char character)
    {
        var args = new TextInputEventArgs(character);
        (FocusedElement as ITextInput)?.OnTextInput(args);
    }

    /// <summary>
    /// Set focus to an element.
    /// </summary>
    public void SetFocus(IUIElement? element)
    {
        if (element == FocusedElement) return;

        FocusedElement?.Blur();
        FocusedElement = element;
        FocusedElement?.Focus();
    }

    /// <summary>
    /// Clear all input state (useful for scene transitions).
    /// </summary>
    public void ClearState()
    {
        _keyboardState.Clear();
        _mouseState.Clear();
        HoveredElement = null;
        DraggedElement = null;
    }

    private static IUIElement? HitTestScene(IScene scene, DuskPoint point)
    {
        // Test elements in reverse order (front to back)
        for (int i = scene.Elements.Count - 1; i >= 0; i--)
        {
            var hit = HitTestElement(scene.Elements[i], point);
            if (hit != null) return hit;
        }
        return null;
    }

    private static IUIElement? HitTestElement(IUIElement element, DuskPoint point)
    {
        if (!element.Visible || !element.Enabled) return null;

        // Test children first (front to back)
        for (int i = element.Children.Count - 1; i >= 0; i--)
        {
            var hit = HitTestElement(element.Children[i], point);
            if (hit != null) return hit;
        }

        // Test this element
        return element.HitTest(point) ? element : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _handlers.Clear();
        _disposed = true;
    }
}

/// <summary>
/// Interface for handling input events.
/// </summary>
public interface IInputHandler
{
    void HandleKeyDown(KeyEventArgs args);
    void HandleKeyUp(KeyEventArgs args);
    void HandleMouseDown(MouseEventArgs args);
    void HandleMouseUp(MouseEventArgs args);
    void HandleMouseWheel(MouseWheelEventArgs args);
}

/// <summary>
/// Interface for draggable elements.
/// </summary>
public interface IDraggable
{
    bool CanDrag { get; }
    void OnDragStart(DragEventArgs args);
    void OnDrag(DragEventArgs args);
    void OnDragEnd(DragEventArgs args);
}

/// <summary>
/// Interface for scrollable elements.
/// </summary>
public interface IScrollable
{
    void OnScroll(int delta);
}

/// <summary>
/// Interface for elements that accept text input.
/// </summary>
public interface ITextInput
{
    void OnTextInput(TextInputEventArgs args);
}

/// <summary>
/// Event args for drag operations.
/// </summary>
public class DragEventArgs : EventArgs
{
    public IUIElement Element { get; }
    public DuskPoint StartPosition { get; }
    public DuskPoint CurrentPosition { get; }
    public DuskPoint Delta => new(CurrentPosition.X - StartPosition.X, CurrentPosition.Y - StartPosition.Y);

    public DragEventArgs(IUIElement element, DuskPoint start, DuskPoint current)
    {
        Element = element;
        StartPosition = start;
        CurrentPosition = current;
    }
}

/// <summary>
/// Event args for text input.
/// </summary>
public class TextInputEventArgs : EventArgs
{
    public char Character { get; }
    public bool Handled { get; set; }

    public TextInputEventArgs(char character)
    {
        Character = character;
    }
}

/// <summary>
/// Event args for mouse wheel.
/// </summary>
public class MouseWheelEventArgs : EventArgs
{
    public DuskPoint Position { get; }
    public int Delta { get; }
    public KeyModifiers Modifiers { get; }
    public bool Handled { get; set; }

    public MouseWheelEventArgs(DuskPoint position, int delta, KeyModifiers modifiers)
    {
        Position = position;
        Delta = delta;
        Modifiers = modifiers;
    }
}
