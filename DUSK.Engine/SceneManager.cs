namespace DUSK.Engine;

using System.Collections.Concurrent;
using DUSK.Core;

/// <summary>
/// Manages scene lifecycle, navigation, and transitions.
/// Central orchestrator for the DUSK application flow.
/// Thread-safe for concurrent scene registration and queries.
/// </summary>
public sealed class SceneManager : IDisposable
{
    private readonly ConcurrentDictionary<string, IScene> _scenes = new();
    private readonly ConcurrentStack<IScene> _sceneStack = new();
    private readonly IRenderer _renderer;
    private readonly object _transitionLock = new();

    private volatile IScene? _activeScene;
    private IScene? _transitionFromScene;
    private IScene? _transitionToScene;
    private SceneTransitionStrategy? _activeTransition;
    private volatile bool _disposed;

    public IScene? ActiveScene => _activeScene;
    public IReadOnlyCollection<IScene> RegisteredScenes => _scenes.Values.ToArray();
    public bool IsTransitioning { get { lock (_transitionLock) return _activeTransition != null && !_activeTransition.IsComplete; } }

    public event EventHandler<SceneChangedEventArgs>? SceneChanged;
    public event EventHandler<SceneChangedEventArgs>? TransitionStarted;
    public event EventHandler<SceneChangedEventArgs>? TransitionCompleted;

    public SceneManager(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Register(IScene scene)
    {
        if (!_scenes.TryAdd(scene.Id, scene))
            throw new InvalidOperationException($"Scene with ID '{scene.Id}' is already registered.");

        scene.Initialize();
    }

    public void Register<T>() where T : IScene, new()
    {
        var scene = new T();
        Register(scene);
    }

    public void Unregister(string sceneId)
    {
        if (_scenes.TryRemove(sceneId, out var scene))
        {
            scene.Dispose();
        }
    }

    public IScene? GetScene(string sceneId)
    {
        return _scenes.GetValueOrDefault(sceneId);
    }

    public T? GetScene<T>(string sceneId) where T : class, IScene
    {
        return GetScene(sceneId) as T;
    }

    public void NavigateTo(string sceneId, TransitionConfig? transition = null)
    {
        if (!_scenes.TryGetValue(sceneId, out var targetScene))
            throw new InvalidOperationException($"Scene '{sceneId}' is not registered.");

        NavigateTo(targetScene, transition);
    }

    public void NavigateTo(IScene scene, TransitionConfig? transition = null)
    {
        if (IsTransitioning) return;

        transition ??= TransitionConfig.StandardFade;

        _transitionFromScene = _activeScene;
        _transitionToScene = scene;

        if (transition.Type == TransitionType.None)
        {
            CompleteTransition();
        }
        else
        {
            _activeTransition = TransitionFactory.Create(transition);
            TransitionStarted?.Invoke(this, new SceneChangedEventArgs(_transitionFromScene, _transitionToScene));
        }
    }

    public void Push(string sceneId, TransitionConfig? transition = null)
    {
        if (!_scenes.TryGetValue(sceneId, out var targetScene))
            throw new InvalidOperationException($"Scene '{sceneId}' is not registered.");

        Push(targetScene, transition);
    }

    public void Push(IScene scene, TransitionConfig? transition = null)
    {
        if (_activeScene != null)
        {
            _sceneStack.Push(_activeScene);
            _activeScene.Hide();
        }

        NavigateTo(scene, transition);
    }

    public void Pop(TransitionConfig? transition = null)
    {
        if (!_sceneStack.TryPop(out var previousScene)) return;

        NavigateTo(previousScene, transition);
    }

    public void PopToRoot(TransitionConfig? transition = null)
    {
        if (!_sceneStack.TryPop(out var rootScene)) return;

        // Pop all remaining scenes, keeping track of the last one as root
        while (_sceneStack.TryPop(out var scene))
        {
            rootScene.Hide();
            rootScene = scene;
        }

        NavigateTo(rootScene, transition);
    }

    public void Update(float deltaTime)
    {
        if (_activeTransition != null && !_activeTransition.IsComplete)
        {
            _activeTransition.Update(deltaTime);

            if (_activeTransition.IsComplete)
            {
                CompleteTransition();
            }
        }
        else
        {
            _activeScene?.Update(deltaTime);
        }
    }

    public void Render()
    {
        _renderer.BeginFrame();

        if (_activeTransition != null && !_activeTransition.IsComplete)
        {
            _activeTransition.Render(_renderer, _transitionFromScene, _transitionToScene);
        }
        else
        {
            _activeScene?.Render(_renderer);
        }

        _renderer.EndFrame();
        _renderer.Present();
    }

    private void CompleteTransition()
    {
        var fromScene = _transitionFromScene;
        var toScene = _transitionToScene;

        fromScene?.Hide();
        _activeScene = toScene;
        toScene?.Show();

        _transitionFromScene = null;
        _transitionToScene = null;
        _activeTransition = null;

        TransitionCompleted?.Invoke(this, new SceneChangedEventArgs(fromScene, toScene));
        SceneChanged?.Invoke(this, new SceneChangedEventArgs(fromScene, toScene));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var scene in _scenes.Values)
        {
            scene.Dispose();
        }
        _scenes.Clear();
        _sceneStack.Clear();
    }
}

public class SceneChangedEventArgs : EventArgs
{
    public IScene? FromScene { get; }
    public IScene? ToScene { get; }

    public SceneChangedEventArgs(IScene? fromScene, IScene? toScene)
    {
        FromScene = fromScene;
        ToScene = toScene;
    }
}
