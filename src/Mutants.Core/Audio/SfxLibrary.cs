namespace Mutants.Core.Audio;

public enum Sfx
{
    Shot,
    EnemyHit,
    EnemyKilled,
    PlayerHurt,
    Coin,
    Key,
    Heart,
    GateOpen,
    BossAwaken,
    BossDefeated,
    LevelCleared,
    LevelStart,
    PlayerDied,
    Victory,
    MenuMove,
    MenuSelect,
    MenuBack,
    SaveRecord,
    Heartbeat,
}

/// <summary>
/// Original sound effects written in the spirit of the 7800's TIA chip: two
/// voices of hard square waves and sample-and-hold noise, short pitch sweeps,
/// and no reverb. None of these are transcriptions of the original game.
/// </summary>
public static class SfxLibrary
{
    public static IReadOnlyList<Sfx> All { get; } = Enum.GetValues<Sfx>();

    public static float[] Build(Sfx sfx)
        => sfx switch
        {
            Sfx.Shot => Shot(),
            Sfx.EnemyHit => EnemyHit(),
            Sfx.EnemyKilled => EnemyKilled(),
            Sfx.PlayerHurt => PlayerHurt(),
            Sfx.Coin => Coin(),
            Sfx.Key => Key(),
            Sfx.Heart => Heart(),
            Sfx.GateOpen => GateOpen(),
            Sfx.BossAwaken => BossAwaken(),
            Sfx.BossDefeated => BossDefeated(),
            Sfx.LevelCleared => LevelCleared(),
            Sfx.LevelStart => LevelStart(),
            Sfx.PlayerDied => PlayerDied(),
            Sfx.Victory => Victory(),
            Sfx.MenuMove => MenuMove(),
            Sfx.MenuSelect => MenuSelect(),
            Sfx.MenuBack => MenuBack(),
            Sfx.SaveRecord => SaveRecord(),
            Sfx.Heartbeat => Heartbeat(),
            _ => throw new ArgumentOutOfRangeException(nameof(sfx), sfx, "Unknown sound effect."),
        };

    private static float[] Shot()
    {
        var buffer = new SynthBuffer(0.11);
        buffer.AddTone(0, 0.09, 880, 260, Waveform.Square, 0.45f, new ToneEnvelope(0.002, 0.03, 16));
        buffer.AddTone(0, 0.05, 440, 180, Waveform.Pulse, 0.18f, new ToneEnvelope(0.001, 0.02, 24), duty: 0.25);
        buffer.AddNoise(0, 0.045, 0.10f, 4200, 1200, new ToneEnvelope(0.001, 0.02, 30));
        return Finish(buffer, 0.55f);
    }

    private static float[] EnemyHit()
    {
        var buffer = new SynthBuffer(0.09);
        buffer.AddTone(0, 0.07, 520, 150, Waveform.Square, 0.4f, new ToneEnvelope(0.001, 0.02, 22));
        buffer.AddNoise(0, 0.04, 0.16f, 3000, 900, new ToneEnvelope(0.001, 0.015, 34));
        return Finish(buffer, 0.5f);
    }

    private static float[] EnemyKilled()
    {
        var buffer = new SynthBuffer(0.34);
        buffer.AddNoise(0, 0.3, 0.6f, 2600, 220, new ToneEnvelope(0.003, 0.08, 4.5));
        buffer.AddTone(0, 0.22, 180, 48, Waveform.Square, 0.32f, new ToneEnvelope(0.002, 0.06, 6));
        buffer.AddTone(0.02, 0.12, 90, 40, Waveform.Triangle, 0.25f, new ToneEnvelope(0.002, 0.05, 8));
        return Finish(buffer, 0.62f);
    }

    private static float[] PlayerHurt()
    {
        var buffer = new SynthBuffer(0.4);
        buffer.AddTone(0, 0.36, 240, 62, Waveform.Saw, 0.42f, new ToneEnvelope(0.004, 0.1, 4));
        buffer.AddTone(0, 0.2, 120, 55, Waveform.Square, 0.22f, new ToneEnvelope(0.002, 0.08, 6));
        buffer.AddNoise(0, 0.12, 0.22f, 900, 200, new ToneEnvelope(0.002, 0.05, 9));
        return Finish(buffer, 0.6f);
    }

    private static float[] Coin()
    {
        var buffer = new SynthBuffer(0.14);
        buffer.AddTone(0, 0.05, 1046, 1046, Waveform.Square, 0.28f, new ToneEnvelope(0.001, 0.012, 30));
        buffer.AddTone(0.055, 0.07, 1568, 1568, Waveform.Square, 0.26f, new ToneEnvelope(0.001, 0.02, 24));
        return Finish(buffer, 0.45f);
    }

    private static float[] Key()
    {
        var buffer = new SynthBuffer(0.5);
        var notes = new[] { 523, 659, 784, 1046 };
        for (var i = 0; i < notes.Length; i++)
        {
            buffer.AddTone(i * 0.085, 0.12, notes[i], notes[i] * 1.002, Waveform.Pulse, 0.26f,
                new ToneEnvelope(0.002, 0.05, 12), duty: 0.35);
        }

        buffer.AddTone(0.34, 0.14, 1568, 1568, Waveform.Triangle, 0.2f, new ToneEnvelope(0.004, 0.08, 6));
        return Finish(buffer, 0.55f);
    }

    private static float[] Heart()
    {
        var buffer = new SynthBuffer(0.42);
        buffer.AddTone(0, 0.16, 392, 415, Waveform.Triangle, 0.32f, new ToneEnvelope(0.006, 0.09, 5));
        buffer.AddTone(0.11, 0.2, 523, 523, Waveform.Triangle, 0.3f, new ToneEnvelope(0.006, 0.1, 4));
        buffer.AddTone(0.22, 0.18, 659, 659, Waveform.Sine, 0.22f, new ToneEnvelope(0.01, 0.12, 3.5));
        return Finish(buffer, 0.55f);
    }

    private static float[] GateOpen()
    {
        var buffer = new SynthBuffer(1.0);
        buffer.AddTone(0, 0.8, 98, 392, Waveform.Square, 0.28f, new ToneEnvelope(0.02, 0.25, 1.4));
        buffer.AddTone(0.1, 0.7, 196, 784, Waveform.Pulse, 0.16f, new ToneEnvelope(0.02, 0.25, 1.8), duty: 0.25);
        buffer.AddNoise(0, 0.9, 0.3f, 400, 3200, new ToneEnvelope(0.05, 0.3, 1.2));
        buffer.AddTone(0, 0.5, 55, 40, Waveform.Sine, 0.4f, new ToneEnvelope(0.004, 0.2, 3));
        return Finish(buffer, 0.7f);
    }

    private static float[] BossAwaken()
    {
        var buffer = new SynthBuffer(1.4);
        buffer.AddTone(0, 1.2, 74, 42, Waveform.Saw, 0.4f, new ToneEnvelope(0.06, 0.4, 0.8), vibratoHz: 6, vibratoDepth: 4);
        buffer.AddTone(0.1, 1.1, 110, 55, Waveform.Square, 0.18f, new ToneEnvelope(0.08, 0.35, 1.2));
        buffer.AddNoise(0, 1.3, 0.35f, 200, 1800, new ToneEnvelope(0.2, 0.5, 0.9));
        return Finish(buffer, 0.72f);
    }

    private static float[] BossDefeated()
    {
        var buffer = new SynthBuffer(1.8);
        buffer.AddNoise(0, 1.6, 0.65f, 3200, 160, new ToneEnvelope(0.002, 0.5, 2.2));
        buffer.AddTone(0, 0.9, 220, 36, Waveform.Saw, 0.35f, new ToneEnvelope(0.004, 0.3, 2.4));
        buffer.AddTone(0.35, 1.2, 150, 30, Waveform.Square, 0.22f, new ToneEnvelope(0.01, 0.4, 2));
        return Finish(buffer, 0.72f);
    }

    private static float[] LevelCleared()
    {
        var buffer = new SynthBuffer(1.15);
        var melody = new[] { 523, 659, 784, 1046 };
        for (var i = 0; i < melody.Length; i++)
        {
            buffer.AddTone(i * 0.14, 0.2, melody[i], melody[i], Waveform.Pulse, 0.26f,
                new ToneEnvelope(0.004, 0.08, 5), duty: 0.4);
            buffer.AddTone(i * 0.14, 0.2, melody[i] / 2.0, melody[i] / 2.0, Waveform.Triangle, 0.18f,
                new ToneEnvelope(0.004, 0.08, 4));
        }

        buffer.AddTone(0.56, 0.5, 1046, 1046, Waveform.Pulse, 0.24f, new ToneEnvelope(0.01, 0.2, 2.2), duty: 0.3);
        buffer.AddTone(0.56, 0.5, 1568, 1568, Waveform.Sine, 0.16f, new ToneEnvelope(0.02, 0.25, 2.4));
        return Finish(buffer, 0.62f);
    }

    private static float[] LevelStart()
    {
        var buffer = new SynthBuffer(0.75);
        buffer.AddTone(0, 0.22, 196, 196, Waveform.Pulse, 0.3f, new ToneEnvelope(0.005, 0.06, 4), duty: 0.25);
        buffer.AddTone(0.16, 0.24, 294, 294, Waveform.Pulse, 0.28f, new ToneEnvelope(0.005, 0.07, 4), duty: 0.25);
        buffer.AddTone(0.32, 0.4, 392, 392, Waveform.Pulse, 0.26f, new ToneEnvelope(0.006, 0.16, 3), duty: 0.25);
        buffer.AddNoise(0.3, 0.35, 0.1f, 1800, 600, new ToneEnvelope(0.05, 0.15, 4));
        return Finish(buffer, 0.55f);
    }

    private static float[] PlayerDied()
    {
        var buffer = new SynthBuffer(1.5);
        var notes = new[] { 415, 349, 294, 220, 165 };
        for (var i = 0; i < notes.Length; i++)
        {
            buffer.AddTone(i * 0.2, 0.34, notes[i], notes[i] * 0.985, Waveform.Square, 0.26f,
                new ToneEnvelope(0.006, 0.14, 3));
        }

        buffer.AddTone(0.9, 0.55, 110, 55, Waveform.Triangle, 0.24f, new ToneEnvelope(0.01, 0.3, 1.6));
        return Finish(buffer, 0.6f);
    }

    private static float[] Victory()
    {
        var buffer = new SynthBuffer(2.4);
        var melody = new[] { 523, 659, 784, 1046, 1318 };
        for (var i = 0; i < melody.Length; i++)
        {
            buffer.AddTone(i * 0.16, 0.24, melody[i], melody[i], Waveform.Pulse, 0.24f,
                new ToneEnvelope(0.004, 0.1, 4.5), duty: 0.4);
            buffer.AddTone(i * 0.16, 0.24, melody[i] / 2.0, melody[i] / 2.0, Waveform.Triangle, 0.16f,
                new ToneEnvelope(0.006, 0.1, 4));
        }

        buffer.AddTone(0.86, 1.2, 1046, 1046, Waveform.Pulse, 0.22f, new ToneEnvelope(0.01, 0.5, 1.4), duty: 0.3, vibratoHz: 5.5, vibratoDepth: 3);
        buffer.AddTone(0.86, 1.2, 1568, 1568, Waveform.Sine, 0.14f, new ToneEnvelope(0.02, 0.5, 1.6));
        buffer.AddNoise(0.86, 0.4, 0.12f, 5000, 2000, new ToneEnvelope(0.05, 0.2, 3));
        return Finish(buffer, 0.66f);
    }

    private static float[] MenuMove()
    {
        var buffer = new SynthBuffer(0.06);
        buffer.AddTone(0, 0.05, 880, 880, Waveform.Square, 0.22f, new ToneEnvelope(0.001, 0.015, 40));
        return Finish(buffer, 0.4f);
    }

    private static float[] MenuSelect()
    {
        var buffer = new SynthBuffer(0.18);
        buffer.AddTone(0, 0.06, 660, 660, Waveform.Square, 0.24f, new ToneEnvelope(0.001, 0.02, 26));
        buffer.AddTone(0.06, 0.1, 990, 990, Waveform.Square, 0.22f, new ToneEnvelope(0.001, 0.04, 18));
        return Finish(buffer, 0.45f);
    }

    private static float[] MenuBack()
    {
        var buffer = new SynthBuffer(0.16);
        buffer.AddTone(0, 0.06, 660, 660, Waveform.Square, 0.22f, new ToneEnvelope(0.001, 0.02, 26));
        buffer.AddTone(0.06, 0.08, 440, 440, Waveform.Square, 0.2f, new ToneEnvelope(0.001, 0.03, 22));
        return Finish(buffer, 0.42f);
    }

    private static float[] SaveRecord()
    {
        var buffer = new SynthBuffer(0.62);
        var notes = new[] { 659, 784, 988, 1318 };
        for (var i = 0; i < notes.Length; i++)
        {
            buffer.AddTone(i * 0.11, 0.16, notes[i], notes[i], Waveform.Pulse, 0.24f,
                new ToneEnvelope(0.003, 0.06, 7), duty: 0.35);
        }

        buffer.AddTone(0.46, 0.14, 1568, 1568, Waveform.Sine, 0.16f, new ToneEnvelope(0.01, 0.08, 5));
        return Finish(buffer, 0.55f);
    }

    private static float[] Heartbeat()
    {
        var buffer = new SynthBuffer(0.55);
        buffer.AddTone(0, 0.12, 64, 46, Waveform.Sine, 0.5f, new ToneEnvelope(0.004, 0.07, 9));
        buffer.AddNoise(0, 0.05, 0.1f, 200, 80, new ToneEnvelope(0.002, 0.02, 20));
        buffer.AddTone(0.18, 0.14, 58, 40, Waveform.Sine, 0.38f, new ToneEnvelope(0.004, 0.08, 9));
        return Finish(buffer, 0.6f);
    }

    /// <summary>Trims to a peak so the mixer has headroom across every effect.</summary>
    private static float[] Finish(SynthBuffer buffer, float peak)
    {
        buffer.RemoveDc();
        buffer.FadeEdges();
        buffer.HardClip();

        // Headroom-friendly peaks are too quiet on a phone speaker, so push the
        // finished effect close to full scale while keeping relative balance.
        buffer.Normalize(MathF.Min(0.95f, peak * 1.35f));
        return buffer.Samples;
    }
}
