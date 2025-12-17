namespace DUSK.DemoApp;

using DUSK.Core;
using DUSK.Engine;
using DUSK.RuntimeAdapters;
using DUSK.Theme;
using DUSK.Sync;

/// <summary>
/// DUSK Framework Demo Application.
/// Demonstrates the Amiga MUI style UI with modern features.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("DUSK Framework Demo");
        Console.WriteLine("==================");
        Console.WriteLine();

        // Initialize cache (would connect to Redis/MongoDB in production)
        Console.WriteLine("Initializing 3-layer cache...");
        var cache = CacheOrchestrator.CreateDefault();
        var syncManager = new SyncCacheManager(cache);
        await syncManager.StartSyncAsync();

        // Initialize theme
        Console.WriteLine("Loading Amiga MUI Classic theme...");
        var themeManager = ThemeManager.Instance;
        themeManager.SetTheme("Amiga MUI Classic");

        // Create renderer
        Console.WriteLine("Creating console renderer...");
        var renderer = RendererFactory.Create(RendererType.Console);
        renderer.Initialize(new RenderConfiguration(80, 24));

        // Create scene manager
        var sceneManager = new SceneManager(renderer);

        // Register scenes
        Console.WriteLine("Registering scenes...");
        sceneManager.Register(new DemoLoginScene());
        sceneManager.Register(new DemoDashboardScene());

        // Navigate to login
        Console.WriteLine();
        Console.WriteLine("Starting demo... (Press Ctrl+C to exit)");
        Console.WriteLine();

        await Task.Delay(1000);

        sceneManager.NavigateTo("login-scene", TransitionConfig.None);

        // Simple game loop
        var running = true;
        var lastTime = DateTime.UtcNow;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            running = false;
        };

        while (running)
        {
            var currentTime = DateTime.UtcNow;
            var deltaTime = (float)(currentTime - lastTime).TotalSeconds;
            lastTime = currentTime;

            // Update
            themeManager.Update(deltaTime);
            sceneManager.Update(deltaTime);

            // Render
            sceneManager.Render();

            // Cap frame rate
            await Task.Delay(16); // ~60fps
        }

        // Cleanup
        Console.WriteLine("\nShutting down...");
        await syncManager.StopSyncAsync();
        sceneManager.Dispose();
        renderer.Dispose();
        syncManager.Dispose();

        Console.WriteLine("Done.");
    }
}
