namespace DUSK.Engine;

using DUSK.Core;

/// <summary>
/// Strategy pattern for scene transitions.
/// Each strategy implements a specific visual transition effect.
/// </summary>
public abstract class SceneTransitionStrategy
{
    public TransitionConfig Config { get; }
    public float Progress { get; protected set; }
    public bool IsComplete => Progress >= 1.0f;

    protected SceneTransitionStrategy(TransitionConfig config)
    {
        Config = config;
    }

    public abstract void Update(float deltaTime);
    public abstract void Render(IRenderer renderer, IScene? fromScene, IScene? toScene);
    public virtual void Reset() => Progress = 0;

    protected float ApplyEasing(float t) => Config.Easing switch
    {
        TransitionEasing.Linear => t,
        TransitionEasing.EaseIn => t * t,
        TransitionEasing.EaseOut => 1 - (1 - t) * (1 - t),
        TransitionEasing.EaseInOut => t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2,
        TransitionEasing.Bounce => BounceEase(t),
        TransitionEasing.Elastic => ElasticEase(t),
        TransitionEasing.Back => BackEase(t),
        TransitionEasing.Cubic => t * t * t,
        TransitionEasing.Sine => 1 - MathF.Cos(t * MathF.PI / 2),
        TransitionEasing.Expo => t == 0 ? 0 : MathF.Pow(2, 10 * (t - 1)),
        _ => t
    };

    private static float BounceEase(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1 / d1) return n1 * t * t;
        if (t < 2 / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
        if (t < 2.5 / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }

    private static float ElasticEase(float t)
    {
        const float c4 = 2 * MathF.PI / 3;
        return t == 0 ? 0 : t == 1 ? 1 : MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * c4) + 1;
    }

    private static float BackEase(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
    }
}

public class FadeTransition : SceneTransitionStrategy
{
    private readonly DuskColor _fadeColor;

    public FadeTransition(TransitionConfig config, DuskColor? fadeColor = null)
        : base(config)
    {
        _fadeColor = fadeColor ?? DuskColor.Black;
    }

    public override void Update(float deltaTime)
    {
        var increment = deltaTime / (float)Config.Duration.TotalSeconds;
        Progress = Math.Min(1.0f, Progress + increment);
    }

    public override void Render(IRenderer renderer, IScene? fromScene, IScene? toScene)
    {
        var easedProgress = ApplyEasing(Progress);

        if (easedProgress < 0.5f)
        {
            // Fade out old scene
            fromScene?.Render(renderer);
            var alpha = (byte)(easedProgress * 2 * 255);
            renderer.DrawRectangle(
                new DuskRect(0, 0, 9999, 9999),
                _fadeColor.WithAlpha(alpha)
            );
        }
        else
        {
            // Fade in new scene
            toScene?.Render(renderer);
            var alpha = (byte)((1 - (easedProgress - 0.5f) * 2) * 255);
            renderer.DrawRectangle(
                new DuskRect(0, 0, 9999, 9999),
                _fadeColor.WithAlpha(alpha)
            );
        }
    }
}

public class SlideTransition : SceneTransitionStrategy
{
    private readonly SlideDirection _direction;

    public enum SlideDirection { Left, Right, Up, Down }

    public SlideTransition(TransitionConfig config, SlideDirection direction)
        : base(config)
    {
        _direction = direction;
    }

    public override void Update(float deltaTime)
    {
        var increment = deltaTime / (float)Config.Duration.TotalSeconds;
        Progress = Math.Min(1.0f, Progress + increment);
    }

    public override void Render(IRenderer renderer, IScene? fromScene, IScene? toScene)
    {
        var easedProgress = ApplyEasing(Progress);
        // Slide transition rendering would offset scenes based on direction and progress
        // Implementation depends on renderer clip region support

        fromScene?.Render(renderer);
        toScene?.Render(renderer);
    }
}

public class CopperBarsTransition : SceneTransitionStrategy
{
    private readonly int _barCount;

    public CopperBarsTransition(TransitionConfig config, int barCount = 16)
        : base(config)
    {
        _barCount = barCount;
    }

    public override void Update(float deltaTime)
    {
        var increment = deltaTime / (float)Config.Duration.TotalSeconds;
        Progress = Math.Min(1.0f, Progress + increment);
    }

    public override void Render(IRenderer renderer, IScene? fromScene, IScene? toScene)
    {
        // Classic Amiga copper bar effect - alternating horizontal bars
        // revealing the new scene progressively

        fromScene?.Render(renderer);

        var barHeight = 9999 / _barCount;
        var revealedBars = (int)(Progress * _barCount);

        for (int i = 0; i < revealedBars; i++)
        {
            var y = i * barHeight;
            renderer.SetClipRegion(new DuskRect(0, y, 9999, barHeight));
            toScene?.Render(renderer);
        }

        renderer.SetClipRegion(null);
    }
}

public class TransitionFactory
{
    public static SceneTransitionStrategy Create(TransitionConfig config) => config.Type switch
    {
        TransitionType.None => new NoTransition(config),
        TransitionType.Fade => new FadeTransition(config),
        TransitionType.FadeToBlack => new FadeTransition(config, DuskColor.Black),
        TransitionType.FadeToWhite => new FadeTransition(config, DuskColor.White),
        TransitionType.SlideLeft => new SlideTransition(config, SlideTransition.SlideDirection.Left),
        TransitionType.SlideRight => new SlideTransition(config, SlideTransition.SlideDirection.Right),
        TransitionType.SlideUp => new SlideTransition(config, SlideTransition.SlideDirection.Up),
        TransitionType.SlideDown => new SlideTransition(config, SlideTransition.SlideDirection.Down),
        TransitionType.CopperBars => new CopperBarsTransition(config),
        _ => new FadeTransition(config)
    };
}

public class NoTransition : SceneTransitionStrategy
{
    public NoTransition(TransitionConfig config) : base(config) { }

    public override void Update(float deltaTime) => Progress = 1.0f;

    public override void Render(IRenderer renderer, IScene? fromScene, IScene? toScene)
    {
        toScene?.Render(renderer);
    }
}
