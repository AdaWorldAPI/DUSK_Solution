namespace DUSK.Theme;

using DUSK.Core;

/// <summary>
/// Core engine for "breathing" UI effects.
/// Provides subtle pulsing animations that respond to system state.
/// Classic Amiga demo scene inspired visual effects.
/// </summary>
public sealed class BreathCore : IDisposable
{
    private readonly IMemoryPulse? _memoryPulse;
    private float _breathPhase;
    private float _breathSpeed = 1.0f;
    private float _breathIntensity = 0.15f;
    private BreathPattern _pattern = BreathPattern.Sine;
    private bool _isRunning;
    private bool _disposed;

    public float CurrentBreathValue { get; private set; }
    public float BreathSpeed
    {
        get => _breathSpeed;
        set => _breathSpeed = Math.Clamp(value, 0.1f, 5.0f);
    }
    public float BreathIntensity
    {
        get => _breathIntensity;
        set => _breathIntensity = Math.Clamp(value, 0f, 1f);
    }
    public BreathPattern Pattern
    {
        get => _pattern;
        set => _pattern = value;
    }

    public bool IsRunning => _isRunning;

    public event EventHandler<BreathEventArgs>? BreathCycle;

    public BreathCore(IMemoryPulse? memoryPulse = null)
    {
        _memoryPulse = memoryPulse;
        if (_memoryPulse != null)
        {
            _memoryPulse.Pulse += OnMemoryPulse;
        }
    }

    public void Start()
    {
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;
    }

    public void Update(float deltaTime)
    {
        if (!_isRunning) return;

        _breathPhase += deltaTime * _breathSpeed;
        if (_breathPhase > MathF.PI * 2)
        {
            _breathPhase -= MathF.PI * 2;
            BreathCycle?.Invoke(this, new BreathEventArgs(CurrentBreathValue));
        }

        CurrentBreathValue = CalculateBreathValue(_breathPhase);
    }

    private float CalculateBreathValue(float phase) => _pattern switch
    {
        BreathPattern.Sine => (MathF.Sin(phase) + 1) / 2 * _breathIntensity,
        BreathPattern.Triangle => MathF.Abs((phase / MathF.PI) % 2 - 1) * _breathIntensity,
        BreathPattern.Pulse => phase < MathF.PI ? _breathIntensity : 0,
        BreathPattern.Heartbeat => CalculateHeartbeat(phase) * _breathIntensity,
        BreathPattern.Copper => CalculateCopperBreath(phase) * _breathIntensity,
        _ => 0
    };

    private static float CalculateHeartbeat(float phase)
    {
        // Two quick beats followed by pause
        var normalized = (phase / (MathF.PI * 2)) % 1;
        if (normalized < 0.1f) return MathF.Sin(normalized * MathF.PI / 0.1f);
        if (normalized < 0.2f) return 0;
        if (normalized < 0.3f) return MathF.Sin((normalized - 0.2f) * MathF.PI / 0.1f) * 0.7f;
        return 0;
    }

    private static float CalculateCopperBreath(float phase)
    {
        // Classic Amiga copper bar style cycling
        var wave1 = MathF.Sin(phase);
        var wave2 = MathF.Sin(phase * 2.3f) * 0.5f;
        var wave3 = MathF.Sin(phase * 0.7f) * 0.3f;
        return (wave1 + wave2 + wave3 + 1.8f) / 3.6f;
    }

    public DuskColor ApplyBreath(DuskColor baseColor)
    {
        var factor = 1.0f + CurrentBreathValue;
        return new DuskColor(
            (byte)Math.Clamp(baseColor.R * factor, 0, 255),
            (byte)Math.Clamp(baseColor.G * factor, 0, 255),
            (byte)Math.Clamp(baseColor.B * factor, 0, 255),
            baseColor.A
        );
    }

    public DuskColor ApplyBreathSubtle(DuskColor baseColor, DuskColor targetColor)
    {
        return baseColor.Lerp(targetColor, CurrentBreathValue);
    }

    public float GetBreathOffset(float baseValue, float range = 2f)
    {
        return baseValue + (CurrentBreathValue - _breathIntensity / 2) * range;
    }

    private void OnMemoryPulse(object? sender, PulseEventArgs e)
    {
        // Adjust breath based on system load
        _breathSpeed = e.Strength switch
        {
            PulseStrength.Idle => 0.5f,
            PulseStrength.Low => 0.75f,
            PulseStrength.Medium => 1.0f,
            PulseStrength.High => 1.5f,
            PulseStrength.Critical => 2.0f,
            _ => 1.0f
        };

        _breathIntensity = e.Strength switch
        {
            PulseStrength.Idle => 0.05f,
            PulseStrength.Low => 0.1f,
            PulseStrength.Medium => 0.15f,
            PulseStrength.High => 0.2f,
            PulseStrength.Critical => 0.3f,
            _ => 0.15f
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_memoryPulse != null)
        {
            _memoryPulse.Pulse -= OnMemoryPulse;
        }
    }
}

public enum BreathPattern
{
    Sine,
    Triangle,
    Pulse,
    Heartbeat,
    Copper
}

public class BreathEventArgs : EventArgs
{
    public float BreathValue { get; }

    public BreathEventArgs(float breathValue)
    {
        BreathValue = breathValue;
    }
}
