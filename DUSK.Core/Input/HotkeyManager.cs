namespace DUSK.Core.Input;

using System.Collections.Concurrent;

/// <summary>
/// Manages global hotkeys and key bindings.
/// Allows registering keyboard shortcuts that trigger actions.
/// Thread-safe for concurrent registration and key handling.
/// </summary>
public sealed class HotkeyManager : IInputHandler, IDisposable
{
    private static HotkeyManager? _instance;
    private static readonly object Lock = new();

    private readonly ConcurrentDictionary<HotkeyBinding, HotkeyAction> _hotkeys = new();
    private readonly ConcurrentDictionary<string, HotkeyBinding> _namedBindings = new();
    private volatile bool _enabled = true;
    private bool _disposed;

    public static HotkeyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    _instance ??= new HotkeyManager();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Enable or disable hotkey processing.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public event EventHandler<HotkeyEventArgs>? HotkeyTriggered;

    private HotkeyManager()
    {
        InputManager.Instance.RegisterHandler(this);
    }

    /// <summary>
    /// Register a hotkey with an action.
    /// </summary>
    public void Register(DuskKey key, KeyModifiers modifiers, Action action, string? name = null)
    {
        var binding = new HotkeyBinding(key, modifiers);
        _hotkeys.AddOrUpdate(binding, new HotkeyAction(action, name), (_, _) => new HotkeyAction(action, name));

        if (!string.IsNullOrEmpty(name))
        {
            _namedBindings.AddOrUpdate(name, binding, (_, _) => binding);
        }
    }

    /// <summary>
    /// Register a hotkey using a string format (e.g., "Ctrl+S", "Alt+F4").
    /// </summary>
    public void Register(string hotkeyString, Action action, string? name = null)
    {
        var binding = HotkeyBinding.Parse(hotkeyString);
        _hotkeys.AddOrUpdate(binding, new HotkeyAction(action, name), (_, _) => new HotkeyAction(action, name));

        if (!string.IsNullOrEmpty(name))
        {
            _namedBindings.AddOrUpdate(name, binding, (_, _) => binding);
        }
    }

    /// <summary>
    /// Unregister a hotkey by binding.
    /// </summary>
    public void Unregister(DuskKey key, KeyModifiers modifiers)
    {
        var binding = new HotkeyBinding(key, modifiers);
        if (_hotkeys.TryRemove(binding, out var action) && action.Name != null)
        {
            _namedBindings.TryRemove(action.Name, out _);
        }
    }

    /// <summary>
    /// Unregister a hotkey by name.
    /// </summary>
    public void Unregister(string name)
    {
        if (_namedBindings.TryRemove(name, out var binding))
        {
            _hotkeys.TryRemove(binding, out _);
        }
    }

    /// <summary>
    /// Clear all registered hotkeys.
    /// </summary>
    public void Clear()
    {
        _hotkeys.Clear();
        _namedBindings.Clear();
    }

    /// <summary>
    /// Get the binding for a named hotkey.
    /// </summary>
    public HotkeyBinding? GetBinding(string name)
    {
        return _namedBindings.TryGetValue(name, out var binding) ? binding : null;
    }

    /// <summary>
    /// Check if a hotkey is registered.
    /// </summary>
    public bool IsRegistered(DuskKey key, KeyModifiers modifiers)
    {
        return _hotkeys.ContainsKey(new HotkeyBinding(key, modifiers));
    }

    /// <summary>
    /// Get all registered hotkeys.
    /// </summary>
    public IEnumerable<(HotkeyBinding Binding, string? Name)> GetAllHotkeys()
    {
        return _hotkeys.Select(kvp => (kvp.Key, kvp.Value.Name));
    }

    public void HandleKeyDown(KeyEventArgs args)
    {
        if (!_enabled || args.Handled) return;

        var binding = new HotkeyBinding(args.Key, args.Modifiers);
        if (_hotkeys.TryGetValue(binding, out var action))
        {
            action.Action();
            HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(binding, action.Name));
            args.Handled = true;
        }
    }

    public void HandleKeyUp(KeyEventArgs args) { }
    public void HandleMouseDown(MouseEventArgs args) { }
    public void HandleMouseUp(MouseEventArgs args) { }
    public void HandleMouseWheel(MouseWheelEventArgs args) { }

    public void Dispose()
    {
        if (_disposed) return;
        InputManager.Instance.UnregisterHandler(this);
        _hotkeys.Clear();
        _namedBindings.Clear();
        _disposed = true;
    }
}

/// <summary>
/// Represents a keyboard shortcut binding.
/// </summary>
public readonly struct HotkeyBinding : IEquatable<HotkeyBinding>
{
    public DuskKey Key { get; }
    public KeyModifiers Modifiers { get; }

    public HotkeyBinding(DuskKey key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Parse a hotkey string like "Ctrl+S" or "Alt+Shift+F4".
    /// </summary>
    public static HotkeyBinding Parse(string hotkeyString)
    {
        var modifiers = KeyModifiers.None;
        var parts = hotkeyString.Split('+');
        var key = DuskKey.None;

        foreach (var part in parts)
        {
            var trimmed = part.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "ctrl":
                case "control":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "alt":
                    modifiers |= KeyModifiers.Alt;
                    break;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "super":
                case "win":
                case "cmd":
                    modifiers |= KeyModifiers.Super;
                    break;
                default:
                    // Try to parse as key
                    if (Enum.TryParse<DuskKey>(trimmed, true, out var parsedKey))
                    {
                        key = parsedKey;
                    }
                    else if (trimmed.Length == 1 && char.IsLetter(trimmed[0]))
                    {
                        // Single letter
                        key = Enum.Parse<DuskKey>(trimmed.ToUpperInvariant());
                    }
                    else if (trimmed.Length == 1 && char.IsDigit(trimmed[0]))
                    {
                        // Single digit
                        key = Enum.Parse<DuskKey>($"D{trimmed}");
                    }
                    break;
            }
        }

        return new HotkeyBinding(key, modifiers);
    }

    public override string ToString()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(KeyModifiers.Super)) parts.Add("Win");

        parts.Add(Key.ToString());

        return string.Join("+", parts);
    }

    public bool Equals(HotkeyBinding other) =>
        Key == other.Key && Modifiers == other.Modifiers;

    public override bool Equals(object? obj) =>
        obj is HotkeyBinding other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Key, Modifiers);

    public static bool operator ==(HotkeyBinding left, HotkeyBinding right) => left.Equals(right);
    public static bool operator !=(HotkeyBinding left, HotkeyBinding right) => !left.Equals(right);
}

/// <summary>
/// Event args for hotkey triggers.
/// </summary>
public class HotkeyEventArgs : EventArgs
{
    public HotkeyBinding Binding { get; }
    public string? Name { get; }

    public HotkeyEventArgs(HotkeyBinding binding, string? name)
    {
        Binding = binding;
        Name = name;
    }
}

internal record HotkeyAction(Action Action, string? Name);

/// <summary>
/// Common hotkey presets for standard application actions.
/// </summary>
public static class StandardHotkeys
{
    public static void RegisterFileOperations(
        Action? newAction = null,
        Action? openAction = null,
        Action? saveAction = null,
        Action? saveAsAction = null,
        Action? closeAction = null)
    {
        var manager = HotkeyManager.Instance;

        if (newAction != null)
            manager.Register("Ctrl+N", newAction, "New");
        if (openAction != null)
            manager.Register("Ctrl+O", openAction, "Open");
        if (saveAction != null)
            manager.Register("Ctrl+S", saveAction, "Save");
        if (saveAsAction != null)
            manager.Register("Ctrl+Shift+S", saveAsAction, "SaveAs");
        if (closeAction != null)
            manager.Register("Ctrl+W", closeAction, "Close");
    }

    public static void RegisterEditOperations(
        Action? undoAction = null,
        Action? redoAction = null,
        Action? cutAction = null,
        Action? copyAction = null,
        Action? pasteAction = null,
        Action? selectAllAction = null)
    {
        var manager = HotkeyManager.Instance;

        if (undoAction != null)
            manager.Register("Ctrl+Z", undoAction, "Undo");
        if (redoAction != null)
            manager.Register("Ctrl+Y", redoAction, "Redo");
        if (cutAction != null)
            manager.Register("Ctrl+X", cutAction, "Cut");
        if (copyAction != null)
            manager.Register("Ctrl+C", copyAction, "Copy");
        if (pasteAction != null)
            manager.Register("Ctrl+V", pasteAction, "Paste");
        if (selectAllAction != null)
            manager.Register("Ctrl+A", selectAllAction, "SelectAll");
    }

    public static void RegisterNavigationOperations(
        Action? findAction = null,
        Action? findNextAction = null,
        Action? replaceAction = null,
        Action? goToAction = null)
    {
        var manager = HotkeyManager.Instance;

        if (findAction != null)
            manager.Register("Ctrl+F", findAction, "Find");
        if (findNextAction != null)
            manager.Register("F3", findNextAction, "FindNext");
        if (replaceAction != null)
            manager.Register("Ctrl+H", replaceAction, "Replace");
        if (goToAction != null)
            manager.Register("Ctrl+G", goToAction, "GoTo");
    }
}
