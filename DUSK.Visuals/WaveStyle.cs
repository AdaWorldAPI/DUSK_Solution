namespace DUSK.Visuals;

using DUSK.Core;

/// <summary>
/// Defines visual wave effect styles.
/// Inspired by Amiga demo scene copper bar effects.
/// </summary>
public record WaveStyle
{
    public WaveType Type { get; init; } = WaveType.Sine;
    public float Amplitude { get; init; } = 10f;
    public float Frequency { get; init; } = 1f;
    public float Speed { get; init; } = 1f;
    public float Phase { get; init; }
    public DuskColor PrimaryColor { get; init; } = DuskColor.AmigaBlue;
    public DuskColor SecondaryColor { get; init; } = DuskColor.AmigaOrange;
    public bool UsePaletteCycle { get; init; }
    public int PaletteSize { get; init; } = 16;

    public static WaveStyle CopperBars => new()
    {
        Type = WaveType.CopperBar,
        Amplitude = 3,
        Frequency = 0.5f,
        Speed = 2f,
        PrimaryColor = DuskColor.AmigaBlue,
        SecondaryColor = new DuskColor(170, 85, 0),
        UsePaletteCycle = true,
        PaletteSize = 32
    };

    public static WaveStyle PlasmaEffect => new()
    {
        Type = WaveType.Plasma,
        Amplitude = 1f,
        Frequency = 2f,
        Speed = 0.5f,
        PrimaryColor = new DuskColor(255, 0, 128),
        SecondaryColor = new DuskColor(0, 255, 128)
    };

    public static WaveStyle SineScroller => new()
    {
        Type = WaveType.Sine,
        Amplitude = 20f,
        Frequency = 3f,
        Speed = 1.5f,
        PrimaryColor = DuskColor.White
    };

    public static WaveStyle SubtlePulse => new()
    {
        Type = WaveType.Sine,
        Amplitude = 2f,
        Frequency = 0.5f,
        Speed = 0.3f,
        PrimaryColor = DuskColor.AmigaGray,
        SecondaryColor = new DuskColor(180, 180, 190)
    };
}

public enum WaveType
{
    Sine,
    Cosine,
    Triangle,
    Square,
    Sawtooth,
    CopperBar,
    Plasma,
    Interference,
    Ripple
}
