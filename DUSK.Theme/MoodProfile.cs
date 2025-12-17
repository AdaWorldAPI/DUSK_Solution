namespace DUSK.Theme;

using DUSK.Core;

/// <summary>
/// Mood-based color profile that can be applied to any theme.
/// Provides emotional color shifts for different application states.
/// </summary>
public class MoodProfile
{
    public ThemeMood Mood { get; }
    public float Intensity { get; set; } = 1.0f;

    // Color modifiers (additive RGB deltas)
    public int RedShift { get; init; }
    public int GreenShift { get; init; }
    public int BlueShift { get; init; }

    // Saturation and brightness modifiers (multiplicative)
    public float SaturationMultiplier { get; init; } = 1.0f;
    public float BrightnessMultiplier { get; init; } = 1.0f;

    // Accent color override
    public DuskColor? AccentColor { get; init; }

    public MoodProfile(ThemeMood mood)
    {
        Mood = mood;
    }

    public DuskColor Apply(DuskColor color)
    {
        var r = Math.Clamp(color.R + (int)(RedShift * Intensity), 0, 255);
        var g = Math.Clamp(color.G + (int)(GreenShift * Intensity), 0, 255);
        var b = Math.Clamp(color.B + (int)(BlueShift * Intensity), 0, 255);

        // Apply brightness
        r = (int)(r * BrightnessMultiplier);
        g = (int)(g * BrightnessMultiplier);
        b = (int)(b * BrightnessMultiplier);

        return new DuskColor(
            (byte)Math.Clamp(r, 0, 255),
            (byte)Math.Clamp(g, 0, 255),
            (byte)Math.Clamp(b, 0, 255),
            color.A
        );
    }

    public static MoodProfile Neutral => new(ThemeMood.Neutral);

    public static MoodProfile Calm => new(ThemeMood.Calm)
    {
        BlueShift = 15,
        GreenShift = 5,
        BrightnessMultiplier = 0.95f
    };

    public static MoodProfile Energetic => new(ThemeMood.Energetic)
    {
        RedShift = 10,
        BrightnessMultiplier = 1.05f,
        AccentColor = DuskColor.AmigaOrange
    };

    public static MoodProfile Focused => new(ThemeMood.Focused)
    {
        BlueShift = 20,
        SaturationMultiplier = 1.1f
    };

    public static MoodProfile Warning => new(ThemeMood.Warning)
    {
        RedShift = 30,
        GreenShift = 15,
        AccentColor = new DuskColor(255, 200, 0)
    };

    public static MoodProfile Error => new(ThemeMood.Error)
    {
        RedShift = 40,
        GreenShift = -10,
        BlueShift = -10,
        AccentColor = new DuskColor(220, 50, 50)
    };

    public static MoodProfile Success => new(ThemeMood.Success)
    {
        GreenShift = 30,
        AccentColor = new DuskColor(50, 180, 50)
    };

    public static MoodProfile GetProfile(ThemeMood mood) => mood switch
    {
        ThemeMood.Neutral => Neutral,
        ThemeMood.Calm => Calm,
        ThemeMood.Energetic => Energetic,
        ThemeMood.Focused => Focused,
        ThemeMood.Warning => Warning,
        ThemeMood.Error => Error,
        ThemeMood.Success => Success,
        _ => Neutral
    };
}
