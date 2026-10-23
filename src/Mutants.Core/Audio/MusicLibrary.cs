namespace Mutants.Core.Audio;

public enum MusicTrack
{
    None,
    Title,
    District,
    Boss,
}

/// <summary>
/// Original looping cues. The 7800 only had two voices, so each loop is built
/// from a bass pulse, one lead line and a noise texture instead of full chords.
/// </summary>
public static class MusicLibrary
{
    public static float[] Build(MusicTrack track, LevelTheme theme = LevelTheme.Graveyard)
        => track switch
        {
            MusicTrack.Title => Title(),
            MusicTrack.District => District(theme),
            MusicTrack.Boss => Boss(),
            _ => throw new ArgumentOutOfRangeException(nameof(track), track, "No loop for that track."),
        };

    public static int RootNote(LevelTheme theme)
        => theme switch
        {
            LevelTheme.Graveyard => 45, // A2
            LevelTheme.Mire => 38,      // D2
            LevelTheme.Foundry => 36,   // C2
            LevelTheme.Chapel => 40,    // E2
            LevelTheme.Clocktower => 42, // F#2
            _ => 45,
        };

    private static float[] Title()
    {
        const double bpm = 84;
        var beat = 60.0 / bpm;
        var buffer = new SynthBuffer(beat * 16);

        // i - VI - III - VII in A minor.
        var roots = new[] { 45, 41, 48, 40 };
        var chords = new[]
        {
            new[] { 57, 60, 64, 69 },
            new[] { 53, 57, 60, 65 },
            new[] { 60, 64, 67, 72 },
            new[] { 52, 55, 59, 64 },
        };

        for (var bar = 0; bar < 4; bar++)
        {
            var barStart = bar * 4 * beat;

            buffer.AddTone(barStart, beat * 1.8, Hz(roots[bar]), Hz(roots[bar]), Waveform.Triangle, 0.38f,
                new ToneEnvelope(0.02, 0.3, 0.7));
            buffer.AddTone(barStart + (beat * 2), beat * 1.6, Hz(roots[bar] + 12), Hz(roots[bar] + 12), Waveform.Square, 0.12f,
                new ToneEnvelope(0.02, 0.3, 1.1));

            for (var step = 0; step < 8; step++)
            {
                var note = chords[bar][step % chords[bar].Length];
                buffer.AddTone(barStart + (step * beat * 0.5), beat * 0.42, Hz(note), Hz(note), Waveform.Pulse, 0.19f,
                    new ToneEnvelope(0.004, 0.12, 3.2), duty: 0.25);
            }

            buffer.AddTone(barStart, beat * 3.6, Hz(chords[bar][3] + 12), Hz(chords[bar][3] + 12), Waveform.Sine, 0.09f,
                new ToneEnvelope(0.25, 0.5, 0.15), vibratoHz: 4.5, vibratoDepth: 2.5);

            buffer.AddNoise(barStart + (beat * 2.5), beat * 1.2, 0.05f, 1600, 900,
                new ToneEnvelope(0.3, 0.4, 0.4));
        }

        return Finish(buffer, 0.55f);
    }

    private static float[] District(LevelTheme theme)
    {
        const double bpm = 138;
        var beat = 60.0 / bpm;
        var root = RootNote(theme);
        var buffer = new SynthBuffer(beat * 16);

        // i - VI - III - VII, a step up in urgency from the title loop.
        var barOffsets = new[] { 0, 8, 3, 10 };

        for (var bar = 0; bar < 4; bar++)
        {
            var barStart = bar * 4 * beat;
            var barRoot = root + barOffsets[bar];

            for (var step = 0; step < 8; step++)
            {
                var accent = step % 2 == 0 ? 0.34f : 0.22f;
                buffer.AddTone(barStart + (step * beat * 0.5), beat * 0.4, Hz(barRoot), Hz(barRoot), Waveform.Square, accent,
                    new ToneEnvelope(0.003, 0.06, 8));
            }

            // Lead arpeggio: root, minor third, fifth, octave and back down.
            var pattern = new[] { 0, 3, 7, 12, 7, 3, 10, 7 };
            for (var step = 0; step < pattern.Length; step++)
            {
                var note = barRoot + 24 + pattern[step];
                buffer.AddTone(barStart + (step * beat * 0.5), beat * 0.34, Hz(note), Hz(note), Waveform.Pulse, 0.16f,
                    new ToneEnvelope(0.002, 0.09, 5), duty: 0.25);
            }

            buffer.AddTone(barStart, beat * 0.18, 92, 48, Waveform.Sine, 0.3f, new ToneEnvelope(0.002, 0.06, 12));
            buffer.AddTone(barStart + (beat * 2), beat * 0.18, 92, 48, Waveform.Sine, 0.26f, new ToneEnvelope(0.002, 0.06, 12));
            buffer.AddNoise(barStart + (beat * 3.5), beat * 0.4, 0.07f, 5200, 2600, new ToneEnvelope(0.002, 0.08, 8));
        }

        return Finish(buffer, 0.52f);
    }

    private static float[] Boss()
    {
        const double bpm = 168;
        var beat = 60.0 / bpm;
        var root = 38; // D2
        var buffer = new SynthBuffer(beat * 16);

        for (var bar = 0; bar < 4; bar++)
        {
            var barStart = bar * 4 * beat;

            for (var step = 0; step < 16; step++)
            {
                var note = root + new[] { 0, 1, 0, 6, 0, 1, 13, 6 }[step % 8];
                var volume = step % 4 == 0 ? 0.24f : 0.13f;
                buffer.AddTone(barStart + (step * beat * 0.25), beat * 0.2, Hz(note), Hz(note), Waveform.Saw, volume,
                    new ToneEnvelope(0.002, 0.05, 9));
            }

            for (var step = 0; step < 8; step++)
            {
                var note = root + 24 + new[] { 0, 6, 12, 6 }[step % 4];
                buffer.AddTone(barStart + (step * beat * 0.5), beat * 0.4, Hz(note), Hz(note), Waveform.Pulse, 0.14f,
                    new ToneEnvelope(0.002, 0.1, 4.5), duty: 0.25);
                buffer.AddNoise(barStart + (step * beat * 0.5), 0.05, 0.12f, 7000, 4500,
                    new ToneEnvelope(0.001, 0.02, 24));
            }

            buffer.AddTone(barStart, beat * 0.5, 55, 34, Waveform.Sine, 0.42f, new ToneEnvelope(0.002, 0.15, 6));
            buffer.AddTone(barStart + (beat * 2), beat * 0.5, 52, 32, Waveform.Sine, 0.38f, new ToneEnvelope(0.002, 0.15, 6));
        }

        return Finish(buffer, 0.55f);
    }

    private static double Hz(int midi)
        => 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);

    private static float[] Finish(SynthBuffer buffer, float peak)
    {
        buffer.RemoveDc();
        buffer.FadeEdges(0.006);
        buffer.HardClip();
        buffer.Normalize(peak);
        return buffer.Samples;
    }
}
