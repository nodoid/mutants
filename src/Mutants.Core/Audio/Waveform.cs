namespace Mutants.Core.Audio;

public enum Waveform
{
    Square,
    Pulse,
    Triangle,
    Saw,
    Sine,
    Noise,
}

/// <summary>
/// Envelope applied to a single tone. Times are in seconds, <see cref="Decay"/>
/// is an exponential decay rate (0 keeps a flat sustain).
/// </summary>
public readonly record struct ToneEnvelope(double Attack = 0.005, double Release = 0.06, double Decay = 0);
