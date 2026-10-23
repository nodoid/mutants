namespace Mutants.Core.Audio;

/// <summary>
/// A tiny offline synthesiser. Everything the game plays is generated here at
/// load time, so the build ships no sampled audio at all.
/// </summary>
public sealed class SynthBuffer
{
    /// <summary>Low enough to keep loops small, high enough for clean square waves.</summary>
    public const int SampleRate = 22050;

    private readonly float[] _samples;

    public SynthBuffer(double seconds)
    {
        if (seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "A buffer needs a positive length.");
        }

        _samples = new float[Math.Max(1, (int)Math.Round(seconds * SampleRate))];
    }

    public int Length => _samples.Length;

    public double Duration => _samples.Length / (double)SampleRate;

    public float[] Samples => _samples;

    public float Peak
    {
        get
        {
            var peak = 0f;
            foreach (var sample in _samples)
            {
                peak = MathF.Max(peak, MathF.Abs(sample));
            }

            return peak;
        }
    }

    /// <summary>
    /// Adds a single tone. The pitch glides linearly from <paramref name="startHz"/>
    /// to <paramref name="endHz"/>, which is how most 8-bit hardware effects were
    /// written: a fast pitch sweep plus an envelope.
    /// </summary>
    public void AddTone(
        double startSeconds,
        double durationSeconds,
        double startHz,
        double endHz,
        Waveform waveform,
        float volume,
        ToneEnvelope? envelope = null,
        double duty = 0.5,
        double vibratoHz = 0,
        double vibratoDepth = 0)
    {
        if (durationSeconds <= 0 || volume == 0f)
        {
            return;
        }

        var env = envelope ?? new ToneEnvelope();
        var start = (int)Math.Round(startSeconds * SampleRate);
        var count = Math.Max(1, (int)Math.Round(durationSeconds * SampleRate));
        var phase = 0.0;
        var noise = new NoiseState(seed: 1);

        for (var i = 0; i < count; i++)
        {
            var index = start + i;
            if (index < 0)
            {
                continue;
            }

            if (index >= _samples.Length)
            {
                break;
            }

            var progress = i / (double)count;
            var hz = startHz + ((endHz - startHz) * progress);
            if (vibratoHz > 0 && vibratoDepth > 0)
            {
                hz += Math.Sin(2 * Math.PI * vibratoHz * (i / (double)SampleRate)) * vibratoDepth;
            }

            hz = Math.Clamp(hz, 8.0, SampleRate / 2.0);

            double sample;
            if (waveform == Waveform.Noise)
            {
                // Sample-and-hold noise can be pitch swept as well.
                sample = noise.Next(hz);
            }
            else
            {
                phase += hz / SampleRate;
                phase -= Math.Floor(phase);
                sample = Oscillate(waveform, phase, duty);
            }

            _samples[index] += (float)(sample * VolumeAt(env, i, count) * volume);
        }
    }

    /// <summary>Adds a filtered noise burst: explosions, static, wind.</summary>
    public void AddNoise(
        double startSeconds,
        double durationSeconds,
        float volume,
        double startHz = 3200,
        double endHz = 600,
        ToneEnvelope? envelope = null,
        int seed = 1)
    {
        var env = envelope ?? new ToneEnvelope(Attack: 0.002, Release: 0.05);
        var start = (int)Math.Round(startSeconds * SampleRate);
        var count = Math.Max(1, (int)Math.Round(durationSeconds * SampleRate));
        var noise = new NoiseState(seed);
        var smoothed = 0f;

        for (var i = 0; i < count; i++)
        {
            var index = start + i;
            if (index < 0)
            {
                continue;
            }

            if (index >= _samples.Length)
            {
                break;
            }

            var progress = i / (double)count;
            var hz = startHz + ((endHz - startHz) * progress);
            var raw = noise.Next(Math.Clamp(hz, 8.0, SampleRate / 2.0));

            // One-pole low pass gives the noise a darker, less hissy body.
            var cutoff = Math.Clamp(hz / (SampleRate * 0.5), 0.01, 0.99);
            smoothed += (float)((raw - smoothed) * cutoff);

            _samples[index] += (float)(smoothed * VolumeAt(env, i, count) * volume);
        }
    }

    /// <summary>Mixes another buffer in, optionally offset and attenuated.</summary>
    public void Mix(SynthBuffer other, double offsetSeconds = 0, float gain = 1f)
    {
        var offset = (int)Math.Round(offsetSeconds * SampleRate);
        for (var i = 0; i < other._samples.Length; i++)
        {
            var index = offset + i;
            if (index < 0)
            {
                continue;
            }

            if (index >= _samples.Length)
            {
                break;
            }

            _samples[index] += other._samples[i] * gain;
        }
    }

    /// <summary>Short fades stop the renderer from clicking at buffer edges.</summary>
    public void FadeEdges(double seconds = 0.004)
    {
        var count = Math.Min((int)Math.Round(seconds * SampleRate), _samples.Length / 2);
        for (var i = 0; i < count; i++)
        {
            var gain = i / (float)count;
            _samples[i] *= gain;
            _samples[_samples.Length - 1 - i] *= gain;
        }
    }

    /// <summary>Scales the whole buffer so nothing clips once converted to PCM.</summary>
    public void Normalize(float peak = 0.9f)
    {
        var current = Peak;
        if (current <= 0f)
        {
            return;
        }

        var gain = peak / current;
        for (var i = 0; i < _samples.Length; i++)
        {
            _samples[i] *= gain;
        }
    }

    /// <summary>
    /// Removes any offset. Narrow pulses and long decay tails bias the average
    /// away from zero, and a biased signal wastes headroom and pops on speakers.
    /// </summary>
    public void RemoveDc()
    {
        double sum = 0;
        foreach (var sample in _samples)
        {
            sum += sample;
        }

        var mean = (float)(sum / _samples.Length);
        if (MathF.Abs(mean) < 1e-6f)
        {
            return;
        }

        for (var i = 0; i < _samples.Length; i++)
        {
            _samples[i] -= mean;
        }
    }

    public void HardClip(float limit = 1f)
    {
        for (var i = 0; i < _samples.Length; i++)
        {
            _samples[i] = Math.Clamp(_samples[i], -limit, limit);
        }
    }

    private static double Oscillate(Waveform waveform, double phase, double duty)
        => waveform switch
        {
            Waveform.Square => phase < 0.5 ? 1.0 : -1.0,
            // Centred so a narrow duty cycle does not add an offset.
            Waveform.Pulse => phase < duty ? 2.0 * (1.0 - duty) : -2.0 * duty,
            Waveform.Triangle => 1.0 - (4.0 * Math.Abs(phase - 0.5)),
            Waveform.Saw => (2.0 * phase) - 1.0,
            Waveform.Sine => Math.Sin(2.0 * Math.PI * phase),
            _ => 0.0,
        };

    private static double VolumeAt(ToneEnvelope envelope, int index, int count)
    {
        var seconds = index / (double)SampleRate;
        var attack = Math.Max(1.0, envelope.Attack * SampleRate);
        var release = Math.Max(1.0, envelope.Release * SampleRate);

        var attackGain = Math.Min(1.0, index / attack);
        var releaseGain = Math.Min(1.0, (count - index) / release);
        var decayGain = envelope.Decay > 0 ? Math.Exp(-envelope.Decay * seconds) : 1.0;

        return attackGain * releaseGain * decayGain;
    }

    /// <summary>Sample-and-hold noise with a phase accumulator, like TIA's noise mode.</summary>
    private struct NoiseState
    {
        private uint _state;
        private double _phase;
        private double _value;

        public NoiseState(int seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : (uint)seed * 2654435761u;
            _phase = 0;
            _value = 0;
        }

        public double Next(double hz)
        {
            _phase += hz / SampleRate;
            while (_phase >= 1.0)
            {
                _phase -= 1.0;
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                _value = ((_state >> 8) / 8388607.5) - 1.0;
            }

            return _value;
        }
    }
}
