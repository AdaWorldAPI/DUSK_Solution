namespace DUSK.Core;

/// <summary>
/// Memory and performance monitoring interface.
/// Provides pulse-based monitoring for the breathing UI effects.
/// </summary>
public interface IMemoryPulse
{
    long TotalMemoryBytes { get; }
    long UsedMemoryBytes { get; }
    long AvailableMemoryBytes { get; }
    double MemoryUsagePercent { get; }

    double CpuUsagePercent { get; }
    int FrameRate { get; }
    double FrameTimeMs { get; }

    PulseStrength CurrentPulseStrength { get; }
    event EventHandler<PulseEventArgs>? Pulse;

    void Start(TimeSpan interval);
    void Stop();
    void RequestPulse();
}

public enum PulseStrength
{
    Idle = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class PulseEventArgs : EventArgs
{
    public PulseStrength Strength { get; }
    public MemorySnapshot Memory { get; }
    public PerformanceSnapshot Performance { get; }
    public DateTime Timestamp { get; }

    public PulseEventArgs(PulseStrength strength, MemorySnapshot memory, PerformanceSnapshot performance)
    {
        Strength = strength;
        Memory = memory;
        Performance = performance;
        Timestamp = DateTime.UtcNow;
    }
}

public record MemorySnapshot(
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes,
    long GCTotalMemory,
    int GCCollectionCount0,
    int GCCollectionCount1,
    int GCCollectionCount2
);

public record PerformanceSnapshot(
    double CpuPercent,
    int FrameRate,
    double FrameTimeMs,
    double RenderTimeMs,
    double UpdateTimeMs,
    int ActiveScenes,
    int TotalElements
);
