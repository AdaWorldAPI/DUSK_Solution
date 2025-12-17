namespace DUSK.Core.Input;

/// <summary>
/// Tracks the current state of the mouse.
/// </summary>
public sealed class MouseState
{
    private readonly HashSet<MouseButton> _pressedButtons = new();
    private readonly Dictionary<MouseButton, DateTime> _buttonPressTime = new();
    private readonly Dictionary<MouseButton, DuskPoint> _buttonPressPosition = new();

    /// <summary>
    /// Current mouse position.
    /// </summary>
    public DuskPoint Position { get; set; }

    /// <summary>
    /// Previous mouse position (from last frame).
    /// </summary>
    public DuskPoint PreviousPosition { get; private set; }

    /// <summary>
    /// Mouse movement delta since last frame.
    /// </summary>
    public DuskPoint Delta => new(Position.X - PreviousPosition.X, Position.Y - PreviousPosition.Y);

    /// <summary>
    /// Current scroll wheel value.
    /// </summary>
    public int ScrollWheelValue { get; set; }

    /// <summary>
    /// Scroll wheel delta since last frame.
    /// </summary>
    public int ScrollDelta { get; private set; }

    /// <summary>
    /// Check if a mouse button is currently pressed.
    /// </summary>
    public bool IsButtonDown(MouseButton button) => _pressedButtons.Contains(button);

    /// <summary>
    /// Check if a mouse button is currently released.
    /// </summary>
    public bool IsButtonUp(MouseButton button) => !_pressedButtons.Contains(button);

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
        if (_buttonPressTime.TryGetValue(button, out var pressTime))
        {
            return DateTime.UtcNow - pressTime;
        }
        return TimeSpan.Zero;
    }

    /// <summary>
    /// Get the position where a button was pressed.
    /// </summary>
    public DuskPoint? GetButtonPressPosition(MouseButton button)
    {
        return _buttonPressPosition.TryGetValue(button, out var pos) ? pos : null;
    }

    /// <summary>
    /// Get the drag distance for a button (from press position to current).
    /// </summary>
    public DuskPoint GetDragDelta(MouseButton button)
    {
        if (_buttonPressPosition.TryGetValue(button, out var pressPos))
        {
            return new DuskPoint(Position.X - pressPos.X, Position.Y - pressPos.Y);
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
        PreviousPosition = Position;
        ScrollDelta = 0;
    }

    internal void SetButtonDown(MouseButton button)
    {
        if (_pressedButtons.Add(button))
        {
            _buttonPressTime[button] = DateTime.UtcNow;
            _buttonPressPosition[button] = Position;
        }
    }

    internal void SetButtonUp(MouseButton button)
    {
        _pressedButtons.Remove(button);
        _buttonPressTime.Remove(button);
        _buttonPressPosition.Remove(button);
    }

    internal void SetScrollDelta(int delta)
    {
        ScrollDelta = delta;
        ScrollWheelValue += delta;
    }

    internal void Clear()
    {
        _pressedButtons.Clear();
        _buttonPressTime.Clear();
        _buttonPressPosition.Clear();
        Position = new DuskPoint(0, 0);
        PreviousPosition = new DuskPoint(0, 0);
        ScrollWheelValue = 0;
        ScrollDelta = 0;
    }
}

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
/// </summary>
public static class CursorManager
{
    private static CursorStyle _currentCursor = CursorStyle.Default;
    private static readonly Stack<CursorStyle> CursorStack = new();

    public static CursorStyle CurrentCursor
    {
        get => _currentCursor;
        set
        {
            if (_currentCursor != value)
            {
                _currentCursor = value;
                CursorChanged?.Invoke(null, value);
            }
        }
    }

    public static event EventHandler<CursorStyle>? CursorChanged;

    /// <summary>
    /// Push a new cursor style onto the stack.
    /// </summary>
    public static void PushCursor(CursorStyle cursor)
    {
        CursorStack.Push(_currentCursor);
        CurrentCursor = cursor;
    }

    /// <summary>
    /// Pop the previous cursor style from the stack.
    /// </summary>
    public static void PopCursor()
    {
        if (CursorStack.Count > 0)
        {
            CurrentCursor = CursorStack.Pop();
        }
    }

    /// <summary>
    /// Reset cursor to default and clear the stack.
    /// </summary>
    public static void ResetCursor()
    {
        CursorStack.Clear();
        CurrentCursor = CursorStyle.Default;
    }
}
