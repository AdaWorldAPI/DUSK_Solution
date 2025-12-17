namespace DUSK.Engine;

using DUSK.Core;

/// <summary>
/// Manages scene lifecycle, navigation, and transitions.
/// Central orchestrator for the DUSK application flow.
/// </summary>
public sealed class SceneManager : IDisposable
{
    private readonly Dictionary<string, IScene> _scenes = new();
    private readonly Stack<IScene> _sceneStack = new();
    private readonly IRenderer _renderer;

    private IScene? _activeScene;
    private IScene? _transitionFromScene;
    private IScene? _transitionToScene;
    private SceneTransitionStrategy? _activeTransition;
    private bool _disposed;

    public IScene? ActiveScene => _activeScene;
    public IReadOnlyCollection<IScene> RegisteredScenes => _scenes.Values;
    public bool IsTransitioning => _activeTransition != null && !_activeTransition.IsComplete;

    public event EventHandler<SceneChangedEventArgs>? SceneChanged;
    public event EventHandler<SceneChangedEventArgs>? TransitionStarted;
    public event EventHandler<SceneChangedEventArgs>? TransitionCompleted;

    public SceneManager(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Register(IScene scene)
    {
        if (_scenes.ContainsKey(scene.Id))
            throw new InvalidOperationException($"Scene with ID '{scene.Id}' is already registered.");

        _scenes[scene.Id] = scene;
        scene.Initialize();
    }

    public void Register<T>() where T : IScene, new()
    {
        var scene = new T();
        Register(scene);
    }

    public void Unregister(string sceneId)
    {
        if (_scenes.TryGetValue(sceneId, out var scene))
        {
            _scenes.Remove(sceneId);
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
        if (_sceneStack.Count == 0) return;

        var previousScene = _sceneStack.Pop();
        NavigateTo(previousScene, transition);
    }

    public void PopToRoot(TransitionConfig? transition = null)
    {
        if (_sceneStack.Count == 0) return;

        IScene rootScene;
        while (_sceneStack.Count > 1)
        {
            _sceneStack.Pop().Hide();
        }
        rootScene = _sceneStack.Pop();

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
