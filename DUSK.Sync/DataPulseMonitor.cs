namespace DUSK.Sync;

using System.Collections.Concurrent;

/// <summary>
/// Monitors database/cache activity and emits pulse data for visualization.
/// Connects the cache layer to the UI waveform display.
/// </summary>
public sealed class DataPulseMonitor : IDisposable
{
    private static DataPulseMonitor? _instance;
    private static readonly object Lock = new();

    private readonly ConcurrentQueue<PulseEvent> _pulseQueue = new();
    private readonly PulseBuffer _l1Buffer = new(128);
    private readonly PulseBuffer _l2Buffer = new(128);
    private readonly PulseBuffer _l3Buffer = new(128);
    private readonly PulseBuffer _combinedBuffer = new(256);

    private readonly Timer _decayTimer;
    private bool _disposed;

    public static DataPulseMonitor Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    _instance ??= new DataPulseMonitor();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Current activity levels (0.0 - 1.0) for each layer.
    /// </summary>
    public float L1Activity { get; private set; }
    public float L2Activity { get; private set; }
    public float L3Activity { get; private set; }
    public float CombinedActivity => (L1Activity * 0.5f + L2Activity * 0.3f + L3Activity * 0.2f);

    /// <summary>
    /// Waveform sample buffers for visualization.
    /// </summary>
    public IReadOnlyList<float> L1Waveform => _l1Buffer.Samples;
    public IReadOnlyList<float> L2Waveform => _l2Buffer.Samples;
    public IReadOnlyList<float> L3Waveform => _l3Buffer.Samples;
    public IReadOnlyList<float> CombinedWaveform => _combinedBuffer.Samples;

    /// <summary>
    /// Statistics
    /// </summary>
    public long TotalL1Hits { get; private set; }
    public long TotalL2Hits { get; private set; }
    public long TotalL3Hits { get; private set; }
    public long TotalMisses { get; private set; }
    public long TotalWrites { get; private set; }

    /// <summary>
    /// Events per second for each layer.
    /// </summary>
    public float L1EventsPerSecond { get; private set; }
    public float L2EventsPerSecond { get; private set; }
    public float L3EventsPerSecond { get; private set; }

    public event EventHandler<PulseEventArgs>? OnPulse;
    public event EventHandler? OnWaveformUpdated;

    private DataPulseMonitor()
    {
        // Decay timer - smoothly reduce activity levels
        _decayTimer = new Timer(DecayTick, null, 16, 16); // ~60fps
    }

    /// <summary>
    /// Record a cache hit event.
    /// </summary>
    public void RecordHit(CacheLayerSource layer, string key, TimeSpan latency)
    {
        var intensity = CalculateIntensity(latency, layer);
        var pulse = new PulseEvent(layer, PulseType.Hit, key, intensity, latency, DateTime.UtcNow);

        _pulseQueue.Enqueue(pulse);
        ProcessPulse(pulse);

        switch (layer)
        {
            case CacheLayerSource.L1_Memory:
                TotalL1Hits++;
                break;
            case CacheLayerSource.L2_Redis:
                TotalL2Hits++;
                break;
            case CacheLayerSource.L3_MongoDB:
                TotalL3Hits++;
                break;
        }

        OnPulse?.Invoke(this, new PulseEventArgs(pulse));
    }

    /// <summary>
    /// Record a cache miss event.
    /// </summary>
    public void RecordMiss(string key, TimeSpan totalLatency)
    {
        TotalMisses++;
        var pulse = new PulseEvent(CacheLayerSource.None, PulseType.Miss, key, 0.3f, totalLatency, DateTime.UtcNow);
        _pulseQueue.Enqueue(pulse);
        ProcessPulse(pulse);
        OnPulse?.Invoke(this, new PulseEventArgs(pulse));
    }

    /// <summary>
    /// Record a write event.
    /// </summary>
    public void RecordWrite(CacheLayerSource layer, string key, TimeSpan latency)
    {
        TotalWrites++;
        var intensity = CalculateIntensity(latency, layer) * 0.7f; // Writes are subtler
        var pulse = new PulseEvent(layer, PulseType.Write, key, intensity, latency, DateTime.UtcNow);

        _pulseQueue.Enqueue(pulse);
        ProcessPulse(pulse);
        OnPulse?.Invoke(this, new PulseEventArgs(pulse));
    }

    /// <summary>
    /// Record an invalidation event.
    /// </summary>
    public void RecordInvalidation(CacheLayerSource layer, string key)
    {
        var pulse = new PulseEvent(layer, PulseType.Invalidation, key, 0.5f, TimeSpan.Zero, DateTime.UtcNow);
        _pulseQueue.Enqueue(pulse);
        ProcessPulse(pulse);
        OnPulse?.Invoke(this, new PulseEventArgs(pulse));
    }

    private void ProcessPulse(PulseEvent pulse)
    {
        // Add to appropriate buffer with waveform shape
        var samples = GenerateWaveSamples(pulse.Intensity, pulse.Type);

        switch (pulse.Layer)
        {
            case CacheLayerSource.L1_Memory:
                _l1Buffer.AddSamples(samples);
                L1Activity = Math.Min(1f, L1Activity + pulse.Intensity);
                break;
            case CacheLayerSource.L2_Redis:
                _l2Buffer.AddSamples(samples);
                L2Activity = Math.Min(1f, L2Activity + pulse.Intensity);
                break;
            case CacheLayerSource.L3_MongoDB:
                _l3Buffer.AddSamples(samples);
                L3Activity = Math.Min(1f, L3Activity + pulse.Intensity);
                break;
        }

        // Combined waveform
        _combinedBuffer.AddSamples(samples.Select(s => s * 0.7f).ToArray());
    }

    private static float[] GenerateWaveSamples(float intensity, PulseType type)
    {
        const int sampleCount = 8;
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float wave = type switch
            {
                PulseType.Hit => MathF.Sin(t * MathF.PI) * intensity,           // Smooth bump
                PulseType.Write => (1f - t) * intensity,                         // Sharp decay
                PulseType.Miss => MathF.Sin(t * MathF.PI * 2) * intensity * 0.5f, // Double bump (sadder)
                PulseType.Invalidation => (t < 0.5f ? t * 2 : 2 - t * 2) * intensity, // Triangle spike
                _ => 0f
            };
            samples[i] = wave;
        }

        return samples;
    }

    private static float CalculateIntensity(TimeSpan latency, CacheLayerSource layer)
    {
        // Faster = more intense (inverse relationship)
        // But also normalize per layer's expected latency
        float expectedMs = layer switch
        {
            CacheLayerSource.L1_Memory => 0.1f,
            CacheLayerSource.L2_Redis => 2f,
            CacheLayerSource.L3_MongoDB => 20f,
            _ => 10f
        };

        float actualMs = (float)latency.TotalMilliseconds;
        float ratio = expectedMs / Math.Max(actualMs, 0.01f);

        return Math.Clamp(ratio, 0.1f, 1f);
    }

    private void DecayTick(object? state)
    {
        const float decayRate = 0.92f;

        L1Activity *= decayRate;
        L2Activity *= decayRate;
        L3Activity *= decayRate;

        // Shift and decay waveform buffers
        _l1Buffer.Decay(decayRate);
        _l2Buffer.Decay(decayRate);
        _l3Buffer.Decay(decayRate);
        _combinedBuffer.Decay(decayRate);

        // Trim old events
        while (_pulseQueue.Count > 1000)
        {
            _pulseQueue.TryDequeue(out _);
        }

        // Calculate events per second (over last second)
        var cutoff = DateTime.UtcNow.AddSeconds(-1);
        var recentEvents = _pulseQueue.Where(p => p.Timestamp > cutoff).ToList();
        L1EventsPerSecond = recentEvents.Count(p => p.Layer == CacheLayerSource.L1_Memory);
        L2EventsPerSecond = recentEvents.Count(p => p.Layer == CacheLayerSource.L2_Redis);
        L3EventsPerSecond = recentEvents.Count(p => p.Layer == CacheLayerSource.L3_MongoDB);

        OnWaveformUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _decayTimer.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Circular buffer for waveform samples.
/// </summary>
internal class PulseBuffer
{
    private readonly float[] _samples;
    private int _writePos;

    public IReadOnlyList<float> Samples => _samples;

    public PulseBuffer(int size)
    {
        _samples = new float[size];
    }

    public void AddSamples(float[] newSamples)
    {
        foreach (var sample in newSamples)
        {
            _samples[_writePos] = Math.Max(_samples[_writePos], sample);
            _writePos = (_writePos + 1) % _samples.Length;
        }
    }

    public void Decay(float factor)
    {
        for (int i = 0; i < _samples.Length; i++)
        {
            _samples[i] *= factor;
            if (_samples[i] < 0.001f) _samples[i] = 0;
        }
    }
}

/// <summary>
/// Cache layer source identifier.
/// </summary>
public enum CacheLayerSource
{
    None,
    L1_Memory,
    L2_Redis,
    L3_MongoDB
}

/// <summary>
/// Type of cache event.
/// </summary>
public enum PulseType
{
    Hit,
    Miss,
    Write,
    Invalidation
}

/// <summary>
/// A single pulse event from the cache.
/// </summary>
public record PulseEvent(
    CacheLayerSource Layer,
    PulseType Type,
    string Key,
    float Intensity,
    TimeSpan Latency,
    DateTime Timestamp
);

/// <summary>
/// Event args for pulse events.
/// </summary>
public class PulseEventArgs : EventArgs
{
    public PulseEvent Pulse { get; }
    public PulseEventArgs(PulseEvent pulse) => Pulse = pulse;
}
