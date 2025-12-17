namespace DUSK.Core.Input;

using System.Collections.Concurrent;

/// <summary>
/// Tracks the current state of the mouse.
/// Thread-safe for concurrent access from input and render threads.
/// </summary>
public sealed class MouseState
{
    private readonly ConcurrentDictionary<MouseButton, ButtonState> _buttonStates = new();
    private DuskPoint _position;
    private DuskPoint _previousPosition;
    private int _scrollWheelValue;
    private int _scrollDelta;
    private readonly object _positionLock = new();

    /// <summary>
    /// Current mouse position.
    /// </summary>
    public DuskPoint Position
    {
        get { lock (_positionLock) return _position; }
        set { lock (_positionLock) _position = value; }
    }

    /// <summary>
    /// Previous mouse position (from last frame).
    /// </summary>
    public DuskPoint PreviousPosition
    {
        get { lock (_positionLock) return _previousPosition; }
        private set { lock (_positionLock) _previousPosition = value; }
    }

    /// <summary>
    /// Mouse movement delta since last frame.
    /// </summary>
    public DuskPoint Delta
    {
        get
        {
            lock (_positionLock)
            {
                return new(_position.X - _previousPosition.X, _position.Y - _previousPosition.Y);
            }
        }
    }

    /// <summary>
    /// Current scroll wheel value.
    /// </summary>
    public int ScrollWheelValue
    {
        get => Interlocked.CompareExchange(ref _scrollWheelValue, 0, 0);
        set => Interlocked.Exchange(ref _scrollWheelValue, value);
    }

    /// <summary>
    /// Scroll wheel delta since last frame.
    /// </summary>
    public int ScrollDelta => Interlocked.CompareExchange(ref _scrollDelta, 0, 0);

    /// <summary>
    /// Check if a mouse button is currently pressed.
    /// </summary>
    public bool IsButtonDown(MouseButton button) => _buttonStates.ContainsKey(button);

    /// <summary>
    /// Check if a mouse button is currently released.
    /// </summary>
    public bool IsButtonUp(MouseButton button) => !_buttonStates.ContainsKey(button);

    /// <summary>
    /// Check if left button is pressed.
    /// </summary>
    public bool LeftButtonDown => IsButtonDown(MouseButton.Left);

    /// <summary>
    /// Check if right button is pressed.
    /// </summary>
    public bool RightButtonDown => IsButtonDown(MouseButton.Right);

    /// <summary>
    /// Check if middle button is pressed.
    /// </summary>
    public bool MiddleButtonDown => IsButtonDown(MouseButton.Middle);

    /// <summary>
    /// Get how long a button has been held down.
    /// </summary>
    public TimeSpan GetButtonHoldDuration(MouseButton button)
    {
        if (_buttonStates.TryGetValue(button, out var state))
        {
            return DateTime.UtcNow - state.PressTime;
        }
        return TimeSpan.Zero;
    }

    /// <summary>
    /// Get the position where a button was pressed.
    /// </summary>
    public DuskPoint? GetButtonPressPosition(MouseButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) ? state.PressPosition : null;
    }

    /// <summary>
    /// Get the drag distance for a button (from press position to current).
    /// </summary>
    public DuskPoint GetDragDelta(MouseButton button)
    {
        if (_buttonStates.TryGetValue(button, out var state))
        {
            var pos = Position;
            return new DuskPoint(pos.X - state.PressPosition.X, pos.Y - state.PressPosition.Y);
        }
        return new DuskPoint(0, 0);
    }

    /// <summary>
    /// Check if dragging with a specific button.
    /// </summary>
    public bool IsDragging(MouseButton button, int threshold = 5)
    {
        if (!IsButtonDown(button)) return false;
        var delta = GetDragDelta(button);
        return Math.Abs(delta.X) > threshold || Math.Abs(delta.Y) > threshold;
    }

    /// <summary>
    /// Update the previous position (call once per frame).
    /// </summary>
    public void UpdatePrevious()
    {
        lock (_positionLock)
        {
            _previousPosition = _position;
        }
        Interlocked.Exchange(ref _scrollDelta, 0);
    }

    internal void SetButtonDown(MouseButton button)
    {
        var pos = Position;
        _buttonStates.TryAdd(button, new ButtonState(DateTime.UtcNow, pos));
    }

    internal void SetButtonUp(MouseButton button)
    {
        _buttonStates.TryRemove(button, out _);
    }

    internal void SetScrollDelta(int delta)
    {
        Interlocked.Exchange(ref _scrollDelta, delta);
        Interlocked.Add(ref _scrollWheelValue, delta);
    }

    internal void Clear()
    {
        _buttonStates.Clear();
        lock (_positionLock)
        {
            _position = new DuskPoint(0, 0);
            _previousPosition = new DuskPoint(0, 0);
        }
        Interlocked.Exchange(ref _scrollWheelValue, 0);
        Interlocked.Exchange(ref _scrollDelta, 0);
    }
}

/// <summary>
/// Internal state for a pressed button.
/// </summary>
internal readonly record struct ButtonState(DateTime PressTime, DuskPoint PressPosition);

/// <summary>
/// Mouse buttons enumeration.
/// </summary>
public enum MouseButton
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 3,
    XButton1 = 4,
    XButton2 = 5
}

/// <summary>
/// Mouse cursor styles.
/// </summary>
public enum CursorStyle
{
    Default,
    Arrow,
    Hand,
    IBeam,
    Wait,
    Cross,
    SizeNS,
    SizeWE,
    SizeNESW,
    SizeNWSE,
    SizeAll,
    No,
    Help,
    Custom
}

/// <summary>
/// Manages the mouse cursor appearance.
/// Thread-safe for concurrent access.
/// </summary>
public static class CursorManager
{
    private static volatile CursorStyle _currentCursor = CursorStyle.Default;
    private static readonly ConcurrentStack<CursorStyle> CursorStack = new();
    private static readonly object _lock = new();

    public static CursorStyle CurrentCursor
    {
        get => _currentCursor;
        set
        {
            lock (_lock)
            {
                if (_currentCursor != value)
                {
                    _currentCursor = value;
                    CursorChanged?.Invoke(null, value);
                }
            }
        }
    }

    public static event EventHandler<CursorStyle>? CursorChanged;

    /// <summary>
    /// Push a new cursor style onto the stack.
    /// </summary>
    public static void PushCursor(CursorStyle cursor)
    {
        lock (_lock)
        {
            CursorStack.Push(_currentCursor);
            _currentCursor = cursor;
            CursorChanged?.Invoke(null, cursor);
        }
    }

    /// <summary>
    /// Pop the previous cursor style from the stack.
    /// </summary>
    public static void PopCursor()
    {
        lock (_lock)
        {
            if (CursorStack.TryPop(out var previous))
            {
                _currentCursor = previous;
                CursorChanged?.Invoke(null, previous);
            }
        }
    }

    /// <summary>
    /// Reset cursor to default and clear the stack.
    /// </summary>
    public static void ResetCursor()
    {
        lock (_lock)
        {
            CursorStack.Clear();
            _currentCursor = CursorStyle.Default;
            CursorChanged?.Invoke(null, CursorStyle.Default);
        }
    }
}
