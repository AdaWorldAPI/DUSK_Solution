# DUSK Framework

**D**rop-in **U**I **S**ystem with **K**lassic style

A modern .NET 8 UI framework inspired by Amiga MUI, designed as a drop-in replacement for Windows Forms with Unity-style scene management and AI-assisted migration tools.

```
    ____  __  ________ __
   / __ \/ / / / ___// //_/
  / / / / / / /\__ \/ ,<
 / /_/ / /_/ /___/ / /| |
/_____/\____//____/_/ |_|

 Amiga MUI meets Modern .NET
```

## Features

### Core Architecture
- **Scene-based UI** - Unity-style scene management replacing WinForms
- **3-Layer Caching** - Memory → Redis → MongoDB with automatic orchestration
- **Multi-renderer** - Console, WPF, and WinForms backends
- **AI-friendly migration** - Tools for automated WinForms conversion

### Theming System
- **Amiga MUI Classic** - Authentic Workbench 3.x appearance
- **Amiga MUI Royale** - Modern take on MUI aesthetics
- **Modern Flat** - Clean, minimal design
- **Classic Win32** - Windows 95/2000 style
- **Mood Profiles** - Context-aware color shifting
- **Breathing Effects** - Subtle pulsing animations

### Visual Effects
- Copper bar transitions (classic Amiga demo scene)
- Plasma effects
- Sine wave scrollers
- Ripple effects

## Quick Start

```csharp
using DUSK.Core;
using DUSK.Engine;
using DUSK.UI;
using DUSK.Theme;
using DUSK.RuntimeAdapters;

// Create renderer
var renderer = RendererFactory.Create(RendererType.Console);
renderer.Initialize(new RenderConfiguration(80, 24));

// Create scene manager
var sceneManager = new SceneManager(renderer);

// Create a scene using fluent builder
public class MyScene : SceneBase
{
    protected override void OnInitialize()
    {
        Theme = ThemeManager.Instance.CurrentTheme;

        UIBuilder.Create()
            .WithTheme(Theme)
            .BeginForm("Login", LayoutMode.Vertical)
                .Label("Username:")
                .TextInput("Enter username")
                .Label("Password:")
                .PasswordInput()
                .Button("Login", OnLogin)
            .EndForm()
            .BuildInto(this);
    }

    private void OnLogin() { /* handle login */ }
}

// Run
sceneManager.Register(new MyScene());
sceneManager.NavigateTo("my-scene");
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      DUSK.DemoApp                           │
├─────────────────────────────────────────────────────────────┤
│  DUSK.Studio  │  DUSK.Migration  │  DUSK.Visuals           │
├───────────────┴──────────────────┴─────────────────────────┤
│              DUSK.RuntimeAdapters                           │
│         (Console | WPF | WinForms)                          │
├─────────────────────────────────────────────────────────────┤
│     DUSK.Engine     │    DUSK.UI    │    DUSK.Theme        │
├─────────────────────┴───────────────┴──────────────────────┤
│                      DUSK.Sync                              │
│           (L1 Memory | L2 Redis | L3 MongoDB)               │
├─────────────────────────────────────────────────────────────┤
│                      DUSK.Core                              │
│    (IRenderer | IScene | ITheme | ICacheProvider)           │
└─────────────────────────────────────────────────────────────┘
```

## 3-Layer Cache System

```
┌─────────────────────────────────────────────────────┐
│                CacheOrchestrator                     │
├─────────────┬─────────────────┬─────────────────────┤
│     L1      │       L2        │         L3          │
│   Memory    │     Redis       │      MongoDB        │
│   <1ms      │    1-5ms        │     10-50ms         │
│   100MB     │     1GB         │    Unlimited        │
└─────────────┴─────────────────┴─────────────────────┘
```

```csharp
var cache = CacheOrchestrator.CreateDefault();

// Automatic read-through: L1 → L2 → L3
var value = await cache.GetAsync<MyData>("key");

// Write-through to all layers
await cache.SetAsync("key", data, CacheEntryOptions.Default);

// Get or create pattern
var result = await cache.GetOrCreateAsync("key",
    async ct => await FetchFromDatabase(ct));
```

## WinForms Migration

DUSK provides tools for AI-assisted migration from WinForms:

```csharp
// Analyze existing form
var analyzer = new WinFormsAnalyzer();
var analysis = analyzer.Analyze(typeof(MyLegacyForm));

// Convert to DUSK scene
var converter = new FormConverter();
var scene = converter.Convert(myFormInstance);

// Generate migration hints for AI
var hints = AIRefactorHints.CreateDefault();
var prompt = hints.GeneratePrompt(analysis);
// Feed prompt to AI for code generation
```

### Control Mappings

| WinForms | DUSK | Notes |
|----------|------|-------|
| `Form` | `SceneBase` | Inherit from SceneBase |
| `Button` | `UIButton` | Full event support |
| `TextBox` | `UIText` | Set `IsEditable = true` |
| `Label` | `UIText` | Set `IsEditable = false` |
| `Panel` | `UIForm` | Set `ShowBorder = false` |
| `GroupBox` | `UIForm` | Set `ShowTitle = true` |

## Theming

```csharp
// Set theme
ThemeManager.Instance.SetTheme("Amiga MUI Classic");

// Apply mood
ThemeManager.Instance.SetMood(ThemeMood.Focused);

// Get themed color with breathing effect
var color = ThemeManager.Instance.ApplyMoodAndBreath(baseColor);

// Create custom theme
public class MyTheme : ThemeProfile
{
    public MyTheme() : base("My Theme", ThemeStyle.Custom)
    {
        SetColor(ThemeColor.Primary, new DuskColor(100, 150, 200));
        SetBevel(ThemeBevelRole.Button, BevelStyle.AmigaMUI);
    }
}
```

## Visual Effects

```csharp
var fxEngine = new WaveFXEngine();

// Add copper bar effect (classic Amiga)
var copperBars = fxEngine.CreateCopperBars(16);

// Add pulsing effect to element
var pulse = fxEngine.CreatePulse(myButton, WaveStyle.SubtlePulse);

// Scrolling text
var scroller = fxEngine.CreateScrollText("DUSK Framework!", font);

// Update in game loop
fxEngine.Update(deltaTime);
fxEngine.RenderEffects(renderer, bounds);
```

## Project Structure

```
DUSK_Solution/
├── DUSK.Core/              # Interfaces and base types
│   ├── IRenderer.cs        # Rendering abstraction
│   ├── IScene.cs           # Scene/element interfaces
│   ├── ITheme.cs           # Theming contracts
│   └── ICacheProvider.cs   # Cache abstraction
│
├── DUSK.Engine/            # Scene management
│   ├── SceneBase.cs        # Base scene implementation
│   ├── SceneManager.cs     # Scene orchestration
│   └── SceneTransitionStrategy.cs  # Transition effects
│
├── DUSK.UI/                # UI components
│   ├── UIElementBase.cs    # Base for all elements
│   ├── UIButton.cs         # Button with MUI bevels
│   ├── UIText.cs           # Text display/input
│   ├── UIForm.cs           # Container with layout
│   └── UIBuilder.cs        # Fluent UI construction
│
├── DUSK.Theme/             # Theming system
│   ├── ThemeProfile.cs     # Theme definition
│   ├── MoodProfile.cs      # Mood-based color shifts
│   ├── BreathCore.cs       # Breathing animations
│   └── Presets/            # Built-in themes
│
├── DUSK.Sync/              # Caching layer
│   ├── CacheOrchestrator.cs    # 3-layer coordination
│   └── Providers/          # L1, L2, L3 implementations
│
├── DUSK.RuntimeAdapters/   # Platform renderers
│   ├── Renderer_Console.cs # Terminal rendering
│   ├── Renderer_WPF.cs     # WPF DrawingVisual
│   └── Renderer_WinForms.cs# GDI+ rendering
│
├── DUSK.Migration/         # WinForms migration
│   ├── WinFormsAnalyzer.cs # Form analysis
│   ├── ControlMapper.cs    # Control conversion
│   └── AIRefactorHints.cs  # AI prompt generation
│
├── DUSK.Visuals/           # Visual effects
│   ├── WaveStyle.cs        # Effect definitions
│   ├── VisualWaveEngine.cs # Wave calculations
│   └── WaveFXEngine.cs     # Effect rendering
│
├── DUSK.Studio/            # Visual editor
│   ├── StudioShell.cs      # Main IDE interface
│   ├── SceneGraphEditor.cs # Hierarchy view
│   └── VisualEditor.cs     # WYSIWYG canvas
│
└── DUSK.DemoApp/           # Demo application
    ├── Program.cs          # Entry point
    ├── DemoLoginScene.cs   # Login demo
    └── DemoDashboardScene.cs # Dashboard demo
```

## Requirements

- .NET 8.0
- Windows (for WPF/WinForms renderers)
- Optional: Redis, MongoDB (for L2/L3 cache)

## NuGet Packages Used

- `StackExchange.Redis` - L2 cache
- `MongoDB.Driver` - L3 cache
- `Microsoft.Extensions.Caching.Memory` - L1 cache

## License

MIT

## Acknowledgments

- Inspired by **Amiga MUI** by Stefan Stuntz
- Scene architecture influenced by **Unity Engine**
- Cache patterns from modern distributed systems

---

*"The Amiga was so far ahead of its time that it took the rest of the world more than a decade to catch up."*
