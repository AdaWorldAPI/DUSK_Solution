namespace DUSK.Core.Input;

using System.Collections.Concurrent;

/// <summary>
/// Tracks the current state of keyboard keys.
/// Thread-safe for concurrent access from input and render threads.
/// </summary>
public sealed class KeyboardState
{
    private readonly ConcurrentDictionary<DuskKey, DateTime> _pressedKeys = new();
    private volatile KeyModifiers _modifiers;
    private readonly object _lock = new();

    /// <summary>
    /// Current modifier keys state.
    /// </summary>
    public KeyModifiers Modifiers
    {
        get => _modifiers;
        set => _modifiers = value;
    }

    /// <summary>
    /// Check if a key is currently pressed.
    /// </summary>
    public bool IsKeyDown(DuskKey key) => _pressedKeys.ContainsKey(key);

    /// <summary>
    /// Check if a key is currently released.
    /// </summary>
    public bool IsKeyUp(DuskKey key) => !_pressedKeys.ContainsKey(key);

    /// <summary>
    /// Get all currently pressed keys.
    /// </summary>
    public IReadOnlyCollection<DuskKey> PressedKeys => _pressedKeys.Keys.ToArray();

    /// <summary>
    /// Check if Shift modifier is active.
    /// </summary>
    public bool IsShiftDown => _modifiers.HasFlag(KeyModifiers.Shift);

    /// <summary>
    /// Check if Control modifier is active.
    /// </summary>
    public bool IsControlDown => _modifiers.HasFlag(KeyModifiers.Control);

    /// <summary>
    /// Check if Alt modifier is active.
    /// </summary>
    public bool IsAltDown => _modifiers.HasFlag(KeyModifiers.Alt);

    /// <summary>
    /// Check if any modifier is active.
    /// </summary>
    public bool HasModifiers => _modifiers != KeyModifiers.None;

    /// <summary>
    /// Get how long a key has been held down.
    /// </summary>
    public TimeSpan GetKeyHoldDuration(DuskKey key)
    {
        if (_pressedKeys.TryGetValue(key, out var pressTime))
        {
            return DateTime.UtcNow - pressTime;
        }
        return TimeSpan.Zero;
    }

    internal void SetKeyDown(DuskKey key)
    {
        _pressedKeys.TryAdd(key, DateTime.UtcNow);
    }

    internal void SetKeyUp(DuskKey key)
    {
        _pressedKeys.TryRemove(key, out _);
    }

    internal void Clear()
    {
        _pressedKeys.Clear();
        _modifiers = KeyModifiers.None;
    }
}

/// <summary>
/// Keyboard keys enumeration.
/// </summary>
public enum DuskKey
{
    None = 0,

    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Numpad
    NumPad0, NumPad1, NumPad2, NumPad3, NumPad4,
    NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,
    NumPadMultiply, NumPadAdd, NumPadSubtract,
    NumPadDecimal, NumPadDivide, NumPadEnter,

    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Arrow keys
    Left, Up, Right, Down,

    // Modifiers
    LeftShift, RightShift,
    LeftControl, RightControl,
    LeftAlt, RightAlt,

    // Special keys
    Space, Enter, Tab, Escape, Backspace, Delete, Insert,
    Home, End, PageUp, PageDown,
    CapsLock, NumLock, ScrollLock, PrintScreen, Pause,

    // Punctuation
    Semicolon, Equals, Comma, Minus, Period, Slash,
    Grave, LeftBracket, Backslash, RightBracket, Quote,

    // Media keys (optional)
    MediaPlay, MediaStop, MediaNext, MediaPrevious,
    VolumeUp, VolumeDown, VolumeMute
}

/// <summary>
/// Keyboard modifiers.
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Super = 8  // Windows/Command key
}
