namespace DUSK.UI;

using DUSK.Core;
using DUSK.Sync;

/// <summary>
/// Winamp-style waveform display showing database cache activity.
/// Renders pulsing bars that "breathe" with data access patterns.
/// </summary>
public class UIWaveformDisplay : UIElementBase
{
    private readonly DataPulseMonitor _monitor;
    private float _time;
    private WaveformStyle _style = WaveformStyle.Bars;
    private WaveformColorScheme _colorScheme = WaveformColorScheme.Amiga;

    public WaveformStyle Style
    {
        get => _style;
        set => _style = value;
    }

    public WaveformColorScheme ColorScheme
    {
        get => _colorScheme;
        set => _colorScheme = value;
    }

    /// <summary>
    /// Which layer to display, or Combined for all.
    /// </summary>
    public WaveformLayer DisplayLayer { get; set; } = WaveformLayer.Combined;

    /// <summary>
    /// Number of bars to display.
    /// </summary>
    public int BarCount { get; set; } = 32;

    /// <summary>
    /// Gap between bars in pixels.
    /// </summary>
    public int BarGap { get; set; } = 2;

    /// <summary>
    /// Mirror the waveform vertically (like Winamp).
    /// </summary>
    public bool Mirror { get; set; } = true;

    /// <summary>
    /// Show layer labels.
    /// </summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>
    /// Show stats overlay.
    /// </summary>
    public bool ShowStats { get; set; } = false;

    /// <summary>
    /// Peak hold time in seconds.
    /// </summary>
    public float PeakHoldTime { get; set; } = 0.5f;

    private readonly float[] _peaks;
    private readonly float[] _peakTimers;

    public UIWaveformDisplay(string? id = null) : base(id)
    {
        _monitor = DataPulseMonitor.Instance;
        _peaks = new float[256];
        _peakTimers = new float[256];
    }

    protected override void OnUpdate(float deltaTime)
    {
        _time += deltaTime;

        // Update peak hold
        for (int i = 0; i < _peaks.Length; i++)
        {
            _peakTimers[i] -= deltaTime;
            if (_peakTimers[i] <= 0)
            {
                _peaks[i] *= 0.95f;
            }
        }
    }

    protected override void OnRender(IRenderer renderer)
    {
        var samples = GetCurrentSamples();
        if (samples.Count == 0) return;

        // Background
        var bgColor = _colorScheme switch
        {
            WaveformColorScheme.Amiga => new DuskColor(0, 0, 40),
            WaveformColorScheme.Winamp => new DuskColor(0, 0, 0),
            WaveformColorScheme.Matrix => new DuskColor(0, 10, 0),
            WaveformColorScheme.Copper => new DuskColor(20, 10, 5),
            _ => DuskColor.Black
        };
        renderer.FillRect(Bounds, bgColor);

        // Scanline effect
        for (int y = 0; y < Bounds.Height; y += 2)
        {
            renderer.DrawLine(
                new DuskPoint(Bounds.X, Bounds.Y + y),
                new DuskPoint(Bounds.X + Bounds.Width, Bounds.Y + y),
                new DuskColor(0, 0, 0, 30)
            );
        }

        switch (_style)
        {
            case WaveformStyle.Bars:
                RenderBars(renderer, samples);
                break;
            case WaveformStyle.Line:
                RenderLine(renderer, samples);
                break;
            case WaveformStyle.Scope:
                RenderScope(renderer, samples);
                break;
            case WaveformStyle.Spectrum:
                RenderSpectrum(renderer, samples);
                break;
        }

        // Layer indicators
        if (ShowLabels)
        {
            RenderLayerIndicators(renderer);
        }

        // Stats overlay
        if (ShowStats)
        {
            RenderStats(renderer);
        }

        // Border
        renderer.DrawRect(Bounds, new DuskColor(80, 80, 100));
    }

    private IReadOnlyList<float> GetCurrentSamples()
    {
        return DisplayLayer switch
        {
            WaveformLayer.L1_Memory => _monitor.L1Waveform,
            WaveformLayer.L2_Redis => _monitor.L2Waveform,
            WaveformLayer.L3_MongoDB => _monitor.L3Waveform,
            WaveformLayer.Combined => _monitor.CombinedWaveform,
            _ => _monitor.CombinedWaveform
        };
    }

    private void RenderBars(IRenderer renderer, IReadOnlyList<float> samples)
    {
        int barWidth = (Bounds.Width - (BarCount - 1) * BarGap) / BarCount;
        if (barWidth < 1) barWidth = 1;

        int centerY = Mirror ? Bounds.Y + Bounds.Height / 2 : Bounds.Y + Bounds.Height;
        int maxHeight = Mirror ? Bounds.Height / 2 - 4 : Bounds.Height - 4;

        for (int i = 0; i < BarCount; i++)
        {
            // Sample from waveform buffer
            int sampleIndex = (int)((float)i / BarCount * samples.Count);
            sampleIndex = Math.Clamp(sampleIndex, 0, samples.Count - 1);
            float value = samples[sampleIndex];

            // Add some ambient motion
            float ambient = MathF.Sin(_time * 2f + i * 0.3f) * 0.05f;
            value = Math.Max(value, ambient);

            // Update peaks
            if (value > _peaks[i])
            {
                _peaks[i] = value;
                _peakTimers[i] = PeakHoldTime;
            }

            int barHeight = (int)(value * maxHeight);
            int peakHeight = (int)(_peaks[i] * maxHeight);

            int barX = Bounds.X + i * (barWidth + BarGap);

            // Get gradient colors
            var (lowColor, midColor, highColor) = GetGradientColors();

            // Draw bar (gradient based on height)
            for (int y = 0; y < barHeight; y++)
            {
                float t = (float)y / maxHeight;
                var color = LerpColor(lowColor, t < 0.6f ? midColor : highColor, t < 0.6f ? t / 0.6f : (t - 0.6f) / 0.4f);

                if (Mirror)
                {
                    // Top half
                    renderer.DrawLine(
                        new DuskPoint(barX, centerY - y),
                        new DuskPoint(barX + barWidth, centerY - y),
                        color
                    );
                    // Bottom half (mirrored)
                    renderer.DrawLine(
                        new DuskPoint(barX, centerY + y),
                        new DuskPoint(barX + barWidth, centerY + y),
                        color
                    );
                }
                else
                {
                    renderer.DrawLine(
                        new DuskPoint(barX, Bounds.Y + Bounds.Height - y),
                        new DuskPoint(barX + barWidth, Bounds.Y + Bounds.Height - y),
                        color
                    );
                }
            }

            // Peak indicator
            if (_peaks[i] > 0.01f)
            {
                var peakColor = highColor with { A = 200 };
                if (Mirror)
                {
                    renderer.DrawLine(
                        new DuskPoint(barX, centerY - peakHeight),
                        new DuskPoint(barX + barWidth, centerY - peakHeight),
                        peakColor
                    );
                    renderer.DrawLine(
                        new DuskPoint(barX, centerY + peakHeight),
                        new DuskPoint(barX + barWidth, centerY + peakHeight),
                        peakColor
                    );
                }
                else
                {
                    renderer.DrawLine(
                        new DuskPoint(barX, Bounds.Y + Bounds.Height - peakHeight),
                        new DuskPoint(barX + barWidth, Bounds.Y + Bounds.Height - peakHeight),
                        peakColor
                    );
                }
            }
        }
    }

    private void RenderLine(IRenderer renderer, IReadOnlyList<float> samples)
    {
        if (samples.Count < 2) return;

        int centerY = Bounds.Y + Bounds.Height / 2;
        int maxAmplitude = Bounds.Height / 2 - 4;
        var (_, midColor, _) = GetGradientColors();

        DuskPoint? lastPoint = null;

        for (int i = 0; i < Bounds.Width; i++)
        {
            int sampleIndex = (int)((float)i / Bounds.Width * samples.Count);
            sampleIndex = Math.Clamp(sampleIndex, 0, samples.Count - 1);
            float value = samples[sampleIndex];

            int y = centerY - (int)(value * maxAmplitude);
            var point = new DuskPoint(Bounds.X + i, y);

            if (lastPoint.HasValue)
            {
                renderer.DrawLine(lastPoint.Value, point, midColor);
            }

            lastPoint = point;
        }
    }

    private void RenderScope(IRenderer renderer, IReadOnlyList<float> samples)
    {
        if (samples.Count < 2) return;

        int centerY = Bounds.Y + Bounds.Height / 2;
        int maxAmplitude = Bounds.Height / 2 - 4;
        var (_, midColor, _) = GetGradientColors();

        // Draw center line
        renderer.DrawLine(
            new DuskPoint(Bounds.X, centerY),
            new DuskPoint(Bounds.X + Bounds.Width, centerY),
            new DuskColor(40, 40, 60)
        );

        // Draw oscilloscope trace
        for (int i = 0; i < Bounds.Width; i++)
        {
            int sampleIndex = (int)((float)i / Bounds.Width * samples.Count);
            sampleIndex = Math.Clamp(sampleIndex, 0, samples.Count - 1);
            float value = samples[sampleIndex];

            // Add oscillation
            float wave = MathF.Sin(_time * 10f + i * 0.1f) * value * 0.3f;
            int y = centerY - (int)((value + wave) * maxAmplitude);

            // Phosphor dot
            renderer.FillRect(new DuskRect(Bounds.X + i, y - 1, 2, 3), midColor);

            // Glow
            renderer.FillRect(new DuskRect(Bounds.X + i - 1, y - 2, 4, 5), midColor with { A = 50 });
        }
    }

    private void RenderSpectrum(IRenderer renderer, IReadOnlyList<float> samples)
    {
        // Fake FFT-like spectrum by grouping and scaling samples
        int bands = Math.Min(BarCount, 64);
        int barWidth = Bounds.Width / bands;
        var (lowColor, midColor, highColor) = GetGradientColors();

        for (int i = 0; i < bands; i++)
        {
            // Average samples for this band
            int startSample = i * samples.Count / bands;
            int endSample = (i + 1) * samples.Count / bands;
            float sum = 0;
            for (int j = startSample; j < endSample && j < samples.Count; j++)
            {
                sum += samples[j];
            }
            float value = sum / Math.Max(1, endSample - startSample);

            // Scale lower frequencies higher (like real spectrum)
            float freqScale = 1f + (1f - (float)i / bands) * 0.5f;
            value *= freqScale;

            int barHeight = (int)(value * (Bounds.Height - 4));
            int barX = Bounds.X + i * barWidth;

            // Gradient fill
            for (int y = 0; y < barHeight; y++)
            {
                float t = (float)y / (Bounds.Height - 4);
                var color = t < 0.5f
                    ? LerpColor(lowColor, midColor, t * 2f)
                    : LerpColor(midColor, highColor, (t - 0.5f) * 2f);

                renderer.DrawLine(
                    new DuskPoint(barX, Bounds.Y + Bounds.Height - y),
                    new DuskPoint(barX + barWidth - BarGap, Bounds.Y + Bounds.Height - y),
                    color
                );
            }
        }
    }

    private void RenderLayerIndicators(IRenderer renderer)
    {
        int indicatorSize = 8;
        int startX = Bounds.X + 4;
        int startY = Bounds.Y + 4;

        // L1 - Memory (fast, green)
        var l1Color = _monitor.L1Activity > 0.1f
            ? new DuskColor(0, (byte)(150 + _monitor.L1Activity * 100), 0)
            : new DuskColor(0, 50, 0);
        renderer.FillRect(new DuskRect(startX, startY, indicatorSize, indicatorSize), l1Color);

        // L2 - Redis (medium, yellow)
        var l2Color = _monitor.L2Activity > 0.1f
            ? new DuskColor((byte)(150 + _monitor.L2Activity * 100), (byte)(150 + _monitor.L2Activity * 100), 0)
            : new DuskColor(50, 50, 0);
        renderer.FillRect(new DuskRect(startX + indicatorSize + 2, startY, indicatorSize, indicatorSize), l2Color);

        // L3 - MongoDB (slow, red)
        var l3Color = _monitor.L3Activity > 0.1f
            ? new DuskColor((byte)(150 + _monitor.L3Activity * 100), 0, 0)
            : new DuskColor(50, 0, 0);
        renderer.FillRect(new DuskRect(startX + (indicatorSize + 2) * 2, startY, indicatorSize, indicatorSize), l3Color);
    }

    private void RenderStats(IRenderer renderer)
    {
        var statsY = Bounds.Y + Bounds.Height - 14;
        var font = new DuskFont("Default", 10);
        var textColor = new DuskColor(150, 150, 180);

        string stats = $"L1:{_monitor.L1EventsPerSecond:F0}/s  L2:{_monitor.L2EventsPerSecond:F0}/s  L3:{_monitor.L3EventsPerSecond:F0}/s";
        renderer.DrawText(stats, new DuskPoint(Bounds.X + 4, statsY), font, textColor);

        string totals = $"Hits:{_monitor.TotalL1Hits + _monitor.TotalL2Hits + _monitor.TotalL3Hits}  Miss:{_monitor.TotalMisses}";
        var textSize = renderer.MeasureText(totals, font);
        renderer.DrawText(totals, new DuskPoint(Bounds.X + Bounds.Width - textSize.Width - 4, statsY), font, textColor);
    }

    private (DuskColor low, DuskColor mid, DuskColor high) GetGradientColors()
    {
        return _colorScheme switch
        {
            WaveformColorScheme.Amiga => (
                new DuskColor(0, 100, 200),    // Blue
                new DuskColor(0, 200, 100),    // Cyan-green
                new DuskColor(255, 100, 0)     // Orange
            ),
            WaveformColorScheme.Winamp => (
                new DuskColor(0, 80, 0),       // Dark green
                new DuskColor(0, 255, 0),      // Bright green
                new DuskColor(255, 255, 0)     // Yellow
            ),
            WaveformColorScheme.Matrix => (
                new DuskColor(0, 40, 0),
                new DuskColor(0, 180, 0),
                new DuskColor(150, 255, 150)
            ),
            WaveformColorScheme.Copper => (
                new DuskColor(100, 50, 20),    // Brown
                new DuskColor(200, 100, 50),   // Copper
                new DuskColor(255, 200, 100)   // Gold
            ),
            WaveformColorScheme.Fire => (
                new DuskColor(80, 0, 0),
                new DuskColor(255, 100, 0),
                new DuskColor(255, 255, 100)
            ),
            _ => (DuskColor.Blue, DuskColor.Green, DuskColor.Red)
        };
    }

    private static DuskColor LerpColor(DuskColor a, DuskColor b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new DuskColor(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }
}

/// <summary>
/// Waveform rendering styles.
/// </summary>
public enum WaveformStyle
{
    Bars,       // Classic Winamp spectrum bars
    Line,       // Simple line graph
    Scope,      // Oscilloscope style
    Spectrum    // Fake FFT spectrum
}

/// <summary>
/// Color schemes for the waveform.
/// </summary>
public enum WaveformColorScheme
{
    Amiga,      // Blue to orange (Workbench vibes)
    Winamp,     // Classic green spectrum
    Matrix,     // Green terminal
    Copper,     // Amiga copper bar colors
    Fire        // Red to yellow
}

/// <summary>
/// Which cache layer to display.
/// </summary>
public enum WaveformLayer
{
    Combined,
    L1_Memory,
    L2_Redis,
    L3_MongoDB
}
