namespace DUSK.Engine;

/// <summary>
/// Defines transition types for scene changes.
/// Inspired by Amiga demo scene transitions and Unity scene management.
/// </summary>
public enum TransitionType
{
    None,
    Fade,
    FadeToBlack,
    FadeToWhite,
    CrossFade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    Push,
    Reveal,
    Dissolve,
    Pixelate,
    CopperBars,      // Classic Amiga copper effect
    HorizontalBlinds,
    VerticalBlinds,
    CircleWipe,
    DiamondWipe,
    Zoom,
    Spin,
    Custom
}

public enum TransitionEasing
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    Bounce,
    Elastic,
    Back,
    Cubic,
    Sine,
    Expo
}

public record TransitionConfig(
    TransitionType Type,
    TimeSpan Duration,
    TransitionEasing Easing = TransitionEasing.EaseInOut
)
{
    public static TransitionConfig None => new(TransitionType.None, TimeSpan.Zero);
    public static TransitionConfig QuickFade => new(TransitionType.Fade, TimeSpan.FromMilliseconds(200));
    public static TransitionConfig StandardFade => new(TransitionType.Fade, TimeSpan.FromMilliseconds(500));
    public static TransitionConfig SlowFade => new(TransitionType.Fade, TimeSpan.FromSeconds(1));
    public static TransitionConfig AmigaCopper => new(TransitionType.CopperBars, TimeSpan.FromMilliseconds(800), TransitionEasing.Linear);
}
