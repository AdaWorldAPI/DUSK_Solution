namespace DUSK.Sync;

using DUSK.Core;

/// <summary>
/// Configuration for sync pulse strength.
/// Adapts sync frequency based on system load and cache activity.
/// </summary>
public sealed class SyncPulseStrength
{
    private readonly IMemoryPulse? _memoryPulse;
    private PulseStrength _currentStrength = PulseStrength.Medium;
    private readonly SyncPulseOptions _options;

    public PulseStrength CurrentStrength => _currentStrength;
    public TimeSpan CurrentInterval => GetIntervalForStrength(_currentStrength);

    public event EventHandler<PulseStrengthChangedEventArgs>? StrengthChanged;

    public SyncPulseStrength(IMemoryPulse? memoryPulse = null, SyncPulseOptions? options = null)
    {
        _memoryPulse = memoryPulse;
        _options = options ?? new SyncPulseOptions();

        if (_memoryPulse != null)
        {
            _memoryPulse.Pulse += OnMemoryPulse;
        }
    }

    public void SetStrength(PulseStrength strength)
    {
        if (_currentStrength == strength) return;

        var old = _currentStrength;
        _currentStrength = strength;
        StrengthChanged?.Invoke(this, new PulseStrengthChangedEventArgs(old, strength));
    }

    public void AdjustForLoad(double cpuPercent, double memoryPercent)
    {
        PulseStrength newStrength;

        if (cpuPercent > 80 || memoryPercent > 90)
        {
            newStrength = PulseStrength.Critical;
        }
        else if (cpuPercent > 60 || memoryPercent > 75)
        {
            newStrength = PulseStrength.High;
        }
        else if (cpuPercent > 40 || memoryPercent > 50)
        {
            newStrength = PulseStrength.Medium;
        }
        else if (cpuPercent > 20 || memoryPercent > 25)
        {
            newStrength = PulseStrength.Low;
        }
        else
        {
            newStrength = PulseStrength.Idle;
        }

        SetStrength(newStrength);
    }

    private void OnMemoryPulse(object? sender, PulseEventArgs e)
    {
        AdjustForLoad(e.Performance.CpuPercent, e.Memory.UsedBytes * 100.0 / e.Memory.TotalBytes);
    }

    private TimeSpan GetIntervalForStrength(PulseStrength strength) => strength switch
    {
        PulseStrength.Idle => _options.IdleInterval,
        PulseStrength.Low => _options.LowInterval,
        PulseStrength.Medium => _options.MediumInterval,
        PulseStrength.High => _options.HighInterval,
        PulseStrength.Critical => _options.CriticalInterval,
        _ => _options.MediumInterval
    };
}

public record SyncPulseOptions
{
    public TimeSpan IdleInterval { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan LowInterval { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan MediumInterval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan HighInterval { get; init; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan CriticalInterval { get; init; } = TimeSpan.FromMilliseconds(100);
}

public class PulseStrengthChangedEventArgs : EventArgs
{
    public PulseStrength OldStrength { get; }
    public PulseStrength NewStrength { get; }

    public PulseStrengthChangedEventArgs(PulseStrength oldStrength, PulseStrength newStrength)
    {
        OldStrength = oldStrength;
        NewStrength = newStrength;
    }
}
