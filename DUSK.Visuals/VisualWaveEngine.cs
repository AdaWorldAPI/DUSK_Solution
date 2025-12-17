namespace DUSK.Visuals;

using DUSK.Core;

/// <summary>
/// Core wave calculation engine for visual effects.
/// Provides real-time wave generation for UI animations.
/// </summary>
public class VisualWaveEngine
{
    private float _time;
    private readonly List<WaveLayer> _layers = new();

    public float Time => _time;
    public IReadOnlyList<WaveLayer> Layers => _layers.AsReadOnly();

    public void AddLayer(WaveStyle style, float weight = 1f)
    {
        _layers.Add(new WaveLayer(style, weight));
    }

    public void RemoveLayer(int index)
    {
        if (index >= 0 && index < _layers.Count)
        {
            _layers.RemoveAt(index);
        }
    }

    public void ClearLayers()
    {
        _layers.Clear();
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
    }

    public void Reset()
    {
        _time = 0;
    }

    public float GetWaveValue(float x, float y = 0)
    {
        if (_layers.Count == 0) return 0;

        float totalValue = 0;
        float totalWeight = 0;

        foreach (var layer in _layers)
        {
            var value = CalculateWave(layer.Style, x, y, _time);
            totalValue += value * layer.Weight;
            totalWeight += layer.Weight;
        }

        return totalWeight > 0 ? totalValue / totalWeight : 0;
    }

    public DuskColor GetWaveColor(float x, float y = 0)
    {
        if (_layers.Count == 0) return DuskColor.Black;

        var waveValue = GetWaveValue(x, y);
        var normalized = (waveValue + 1) / 2; // Normalize to 0-1

        // Blend between primary and secondary colors based on wave value
        var layer = _layers[0];
        return layer.Style.PrimaryColor.Lerp(layer.Style.SecondaryColor, normalized);
    }

    public float[] GetWaveArray(int length, float startX = 0, float y = 0)
    {
        var result = new float[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = GetWaveValue(startX + i, y);
        }
        return result;
    }

    public DuskColor[] GetColorArray(int length, float startX = 0, float y = 0)
    {
        var result = new DuskColor[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = GetWaveColor(startX + i, y);
        }
        return result;
    }

    private float CalculateWave(WaveStyle style, float x, float y, float time)
    {
        var phase = style.Phase + time * style.Speed;
        var input = (x * style.Frequency / 100f) + phase;

        return style.Type switch
        {
            WaveType.Sine => MathF.Sin(input) * style.Amplitude,
            WaveType.Cosine => MathF.Cos(input) * style.Amplitude,
            WaveType.Triangle => TriangleWave(input) * style.Amplitude,
            WaveType.Square => SquareWave(input) * style.Amplitude,
            WaveType.Sawtooth => SawtoothWave(input) * style.Amplitude,
            WaveType.CopperBar => CopperBarWave(x, y, time, style),
            WaveType.Plasma => PlasmaWave(x, y, time, style),
            WaveType.Interference => InterferenceWave(x, y, time, style),
            WaveType.Ripple => RippleWave(x, y, time, style),
            _ => MathF.Sin(input) * style.Amplitude
        };
    }

    private static float TriangleWave(float x)
    {
        return 2 * MathF.Abs(2 * ((x / MathF.Tau) - MathF.Floor((x / MathF.Tau) + 0.5f))) - 1;
    }

    private static float SquareWave(float x)
    {
        return MathF.Sin(x) >= 0 ? 1 : -1;
    }

    private static float SawtoothWave(float x)
    {
        return 2 * ((x / MathF.Tau) - MathF.Floor((x / MathF.Tau) + 0.5f));
    }

    private static float CopperBarWave(float x, float y, float time, WaveStyle style)
    {
        // Classic Amiga copper bar - horizontal bars with color cycling
        var barIndex = (int)((y + time * style.Speed * 50) / 8) % style.PaletteSize;
        var intensity = MathF.Sin((barIndex * MathF.Tau / style.PaletteSize) + time * style.Speed);
        return intensity * style.Amplitude;
    }

    private static float PlasmaWave(float x, float y, float time, WaveStyle style)
    {
        // Classic plasma effect - multiple sine waves combined
        var v1 = MathF.Sin(x / 16f + time * style.Speed);
        var v2 = MathF.Sin(y / 8f + time * style.Speed);
        var v3 = MathF.Sin((x + y) / 16f + time * style.Speed);
        var v4 = MathF.Sin(MathF.Sqrt(x * x + y * y) / 8f + time * style.Speed);

        return ((v1 + v2 + v3 + v4) / 4) * style.Amplitude;
    }

    private static float InterferenceWave(float x, float y, float time, WaveStyle style)
    {
        // Two wave sources creating interference pattern
        var source1 = MathF.Sqrt(x * x + y * y);
        var source2 = MathF.Sqrt((x - 100) * (x - 100) + (y - 50) * (y - 50));

        var wave1 = MathF.Sin(source1 * style.Frequency / 10f - time * style.Speed);
        var wave2 = MathF.Sin(source2 * style.Frequency / 10f - time * style.Speed);

        return ((wave1 + wave2) / 2) * style.Amplitude;
    }

    private static float RippleWave(float x, float y, float time, WaveStyle style)
    {
        // Expanding circular ripple
        var distance = MathF.Sqrt(x * x + y * y);
        var ripple = MathF.Sin(distance * style.Frequency / 5f - time * style.Speed * 3);
        var fade = MathF.Max(0, 1 - distance / 200);

        return ripple * fade * style.Amplitude;
    }
}

public record WaveLayer(WaveStyle Style, float Weight = 1f);
