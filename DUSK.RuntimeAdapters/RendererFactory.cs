namespace DUSK.RuntimeAdapters;

using System.Collections.Concurrent;
using DUSK.Core;

/// <summary>
/// Factory for creating platform-specific renderers.
/// Enables runtime selection of rendering backend.
/// Thread-safe for concurrent registration and creation.
/// </summary>
public static class RendererFactory
{
    private static readonly ConcurrentDictionary<RendererType, Func<IRenderer>> _factories = new();
    private static RendererType _defaultType = RendererType.Console;

    static RendererFactory()
    {
        // Register built-in renderers
        Register(RendererType.Console, () => new Renderer_Console());

        // Platform-specific renderers registered conditionally
        if (OperatingSystem.IsWindows())
        {
            Register(RendererType.WinForms, () => new Renderer_WinForms());
            Register(RendererType.WPF, () => new Renderer_WPF());
        }
    }

    public static void Register(RendererType type, Func<IRenderer> factory)
    {
        _factories.AddOrUpdate(type, factory, (_, _) => factory);
    }

    public static void SetDefault(RendererType type)
    {
        if (!_factories.ContainsKey(type))
            throw new ArgumentException($"Renderer type '{type}' is not registered.");

        _defaultType = type;
    }

    public static IRenderer Create()
    {
        return Create(_defaultType);
    }

    public static IRenderer Create(RendererType type)
    {
        if (_factories.TryGetValue(type, out var factory))
        {
            return factory();
        }

        throw new ArgumentException($"Renderer type '{type}' is not registered.");
    }

    public static IRenderer CreateBest()
    {
        // Try to create the best available renderer for the platform
        if (OperatingSystem.IsWindows())
        {
            // Prefer WPF on Windows
            if (_factories.ContainsKey(RendererType.WPF))
                return Create(RendererType.WPF);

            if (_factories.ContainsKey(RendererType.WinForms))
                return Create(RendererType.WinForms);
        }

        return Create(RendererType.Console);
    }

    public static bool IsAvailable(RendererType type)
    {
        return _factories.ContainsKey(type);
    }

    public static IEnumerable<RendererType> GetAvailableRenderers()
    {
        return _factories.Keys;
    }
}

public enum RendererType
{
    Console,
    WinForms,
    WPF,
    Unity,
    Custom
}
