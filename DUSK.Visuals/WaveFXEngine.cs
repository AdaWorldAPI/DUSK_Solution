namespace DUSK.Visuals;

using DUSK.Core;

/// <summary>
/// High-level visual effects engine combining waves with rendering.
/// Provides ready-to-use effects for UI elements.
/// </summary>
public class WaveFXEngine : IDisposable
{
    private readonly VisualWaveEngine _waveEngine = new();
    private readonly List<VisualEffect> _activeEffects = new();
    private bool _disposed;

    public bool IsRunning { get; private set; }
    public VisualWaveEngine WaveEngine => _waveEngine;

    public event EventHandler<EffectFrameEventArgs>? FrameRendered;

    public void Start()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Update(float deltaTime)
    {
        if (!IsRunning) return;

        _waveEngine.Update(deltaTime);

        foreach (var effect in _activeEffects)
        {
            effect.Update(deltaTime);
        }
    }

    public void AddEffect(VisualEffect effect)
    {
        _activeEffects.Add(effect);
    }

    public void RemoveEffect(VisualEffect effect)
    {
        _activeEffects.Remove(effect);
    }

    public void RenderEffects(IRenderer renderer, DuskRect bounds)
    {
        if (!IsRunning) return;

        foreach (var effect in _activeEffects)
        {
            if (effect.IsActive)
            {
                effect.Render(renderer, bounds, _waveEngine);
            }
        }

        FrameRendered?.Invoke(this, new EffectFrameEventArgs(_waveEngine.Time));
    }

    public CopperBarEffect CreateCopperBars(int barCount = 16)
    {
        var effect = new CopperBarEffect(barCount);
        AddEffect(effect);
        return effect;
    }

    public PulseEffect CreatePulse(IUIElement target, WaveStyle? style = null)
    {
        var effect = new PulseEffect(target, style ?? WaveStyle.SubtlePulse);
        AddEffect(effect);
        return effect;
    }

    public ScrollTextEffect CreateScrollText(string text, DuskFont font)
    {
        var effect = new ScrollTextEffect(text, font);
        AddEffect(effect);
        return effect;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _activeEffects.Clear();
    }
}

public abstract class VisualEffect
{
    public bool IsActive { get; set; } = true;
    public float Duration { get; set; } = float.MaxValue;
    public float ElapsedTime { get; protected set; }

    public virtual void Update(float deltaTime)
    {
        ElapsedTime += deltaTime;
        if (ElapsedTime >= Duration)
        {
            IsActive = false;
        }
    }

    public abstract void Render(IRenderer renderer, DuskRect bounds, VisualWaveEngine waveEngine);

    public virtual void Reset()
    {
        ElapsedTime = 0;
        IsActive = true;
    }
}

public class CopperBarEffect : VisualEffect
{
    private readonly int _barCount;
    private readonly WaveStyle _style;

    public DuskColor[] Palette { get; }
    public float ScrollSpeed { get; set; } = 50f;

    public CopperBarEffect(int barCount = 16)
    {
        _barCount = barCount;
        _style = WaveStyle.CopperBars;
        Palette = GenerateCopperPalette(barCount);
    }

    public override void Render(IRenderer renderer, DuskRect bounds, VisualWaveEngine waveEngine)
    {
        var barHeight = bounds.Height / _barCount;
        var offset = (int)(waveEngine.Time * ScrollSpeed) % barHeight;

        for (int i = -1; i <= _barCount; i++)
        {
            var y = bounds.Y + i * barHeight - offset;
            if (y < bounds.Y - barHeight || y > bounds.Bottom) continue;

            var paletteIndex = (i + (int)(waveEngine.Time * 2)) % Palette.Length;
            if (paletteIndex < 0) paletteIndex += Palette.Length;

            var color = Palette[paletteIndex];
            renderer.DrawRectangle(new DuskRect(bounds.X, y, bounds.Width, barHeight), color);
        }
    }

    private static DuskColor[] GenerateCopperPalette(int size)
    {
        var palette = new DuskColor[size];
        for (int i = 0; i < size; i++)
        {
            var t = (float)i / size;
            var r = (byte)(128 + 127 * MathF.Sin(t * MathF.Tau));
            var g = (byte)(128 + 127 * MathF.Sin(t * MathF.Tau + MathF.Tau / 3));
            var b = (byte)(128 + 127 * MathF.Sin(t * MathF.Tau + 2 * MathF.Tau / 3));
            palette[i] = new DuskColor(r, g, b);
        }
        return palette;
    }
}

public class PulseEffect : VisualEffect
{
    private readonly IUIElement _target;
    private readonly WaveStyle _style;
    private readonly DuskColor _originalColor;

    public float Intensity { get; set; } = 0.2f;

    public PulseEffect(IUIElement target, WaveStyle style)
    {
        _target = target;
        _style = style;
        _originalColor = DuskColor.AmigaGray; // Would get from target
    }

    public override void Render(IRenderer renderer, DuskRect bounds, VisualWaveEngine waveEngine)
    {
        var pulse = waveEngine.GetWaveValue(ElapsedTime * 100);
        var normalizedPulse = (pulse / _style.Amplitude + 1) / 2;
        var intensity = normalizedPulse * Intensity;

        // Apply pulse as overlay
        var overlayColor = _style.PrimaryColor.WithAlpha((byte)(intensity * 255));
        renderer.DrawRectangle(_target.Bounds, overlayColor);
    }
}

public class ScrollTextEffect : VisualEffect
{
    private readonly string _text;
    private readonly DuskFont _font;
    private float _xOffset;

    public float ScrollSpeed { get; set; } = 100f;
    public float WaveAmplitude { get; set; } = 20f;
    public DuskColor TextColor { get; set; } = DuskColor.White;

    public ScrollTextEffect(string text, DuskFont font)
    {
        _text = text;
        _font = font;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _xOffset += ScrollSpeed * deltaTime;
    }

    public override void Render(IRenderer renderer, DuskRect bounds, VisualWaveEngine waveEngine)
    {
        var textWidth = _text.Length * _font.Size; // Approximate

        // Reset scroll when text goes off screen
        if (_xOffset > bounds.Width + textWidth)
        {
            _xOffset = 0;
        }

        // Draw each character with sine wave offset
        for (int i = 0; i < _text.Length; i++)
        {
            var x = bounds.Right - _xOffset + i * _font.Size;
            if (x < bounds.X - _font.Size || x > bounds.Right) continue;

            var yOffset = MathF.Sin((x + ElapsedTime * 200) / 50f) * WaveAmplitude;
            var y = bounds.Y + bounds.Height / 2 + (int)yOffset;

            renderer.DrawText(_text[i].ToString(), new DuskPoint((int)x, y), _font, TextColor);
        }
    }
}

public class EffectFrameEventArgs : EventArgs
{
    public float Time { get; }

    public EffectFrameEventArgs(float time)
    {
        Time = time;
    }
}
