using Mutants.Core;
using Mutants.Core.Audio;

namespace Mutants.Core.Tests;

public sealed class AudioSynthTests
{
    [Fact]
    public void BufferLengthMatchesRequestedDuration()
    {
        var buffer = new SynthBuffer(0.5);

        Assert.Equal(SynthBuffer.SampleRate / 2, buffer.Length);
        Assert.Equal(0.5, buffer.Duration, precision: 4);
    }

    [Fact]
    public void ToneProducesExpectedPitch()
    {
        var buffer = new SynthBuffer(0.5);
        buffer.AddTone(0, 0.5, 440, 440, Waveform.Square, 0.8f, new ToneEnvelope(0.001, 0.001));

        var crossings = CountZeroCrossings(buffer.Samples, 0, buffer.Length);

        // A square wave at 440 Hz crosses twice per cycle.
        Assert.InRange(crossings, 440 - 12, 440 + 12);
    }

    [Fact]
    public void PitchSweepLowersFrequencyOverTime()
    {
        var buffer = new SynthBuffer(0.4);
        buffer.AddTone(0, 0.4, 880, 220, Waveform.Square, 0.8f, new ToneEnvelope(0.001, 0.001));

        var window = SynthBuffer.SampleRate / 10;
        var early = CountZeroCrossings(buffer.Samples, 0, window);
        var late = CountZeroCrossings(buffer.Samples, buffer.Length - window, buffer.Length);

        Assert.True(late < early, $"expected the sweep to fall (early {early}, late {late})");
    }

    [Fact]
    public void NoiseIsDeterministicForASeed()
    {
        var first = new SynthBuffer(0.2);
        first.AddNoise(0, 0.2, 0.5f, 2000, 800, seed: 7);

        var second = new SynthBuffer(0.2);
        second.AddNoise(0, 0.2, 0.5f, 2000, 800, seed: 7);

        var other = new SynthBuffer(0.2);
        other.AddNoise(0, 0.2, 0.5f, 2000, 800, seed: 8);

        Assert.Equal(first.Samples, second.Samples);
        Assert.NotEqual(first.Samples, other.Samples);
    }

    [Fact]
    public void FadeEdgesSilencesTheBufferBoundaries()
    {
        var buffer = new SynthBuffer(0.2);
        buffer.AddTone(0, 0.2, 300, 300, Waveform.Square, 0.9f, new ToneEnvelope(0, 0));

        buffer.FadeEdges(0.01);

        Assert.True(MathF.Abs(buffer.Samples[0]) < 0.05f);
        Assert.True(MathF.Abs(buffer.Samples[^1]) < 0.05f);
    }

    [Fact]
    public void NormalizeScalesToTheRequestedPeak()
    {
        var buffer = new SynthBuffer(0.1);
        buffer.AddTone(0, 0.1, 440, 440, Waveform.Square, 2f, new ToneEnvelope(0, 0));
        buffer.Normalize(0.5f);

        Assert.Equal(0.5f, buffer.Peak, precision: 3);
    }

    [Theory]
    [InlineData(Sfx.Shot)]
    [InlineData(Sfx.EnemyHit)]
    [InlineData(Sfx.EnemyKilled)]
    [InlineData(Sfx.PlayerHurt)]
    [InlineData(Sfx.Coin)]
    [InlineData(Sfx.Key)]
    [InlineData(Sfx.Heart)]
    [InlineData(Sfx.GateOpen)]
    [InlineData(Sfx.BossAwaken)]
    [InlineData(Sfx.BossDefeated)]
    [InlineData(Sfx.LevelCleared)]
    [InlineData(Sfx.LevelStart)]
    [InlineData(Sfx.PlayerDied)]
    [InlineData(Sfx.Victory)]
    [InlineData(Sfx.MenuMove)]
    [InlineData(Sfx.MenuSelect)]
    [InlineData(Sfx.MenuBack)]
    [InlineData(Sfx.SaveRecord)]
    [InlineData(Sfx.Heartbeat)]
    public void EveryEffectIsAudibleAndWithinRange(Sfx sfx)
    {
        var samples = SfxLibrary.Build(sfx);

        Assert.NotEmpty(samples);
        Assert.All(samples, sample => Assert.InRange(sample, -1f, 1f));
        Assert.True(Peak(samples) > 0.2f, $"{sfx} is too quiet to hear");
    }

    [Fact]
    public void EffectsAreDeterministic()
    {
        foreach (var sfx in SfxLibrary.All)
        {
            Assert.Equal(SfxLibrary.Build(sfx), SfxLibrary.Build(sfx));
        }
    }

    [Fact]
    public void EveryEffectFinishesSilentSoChannelsAreNotHeld()
    {
        foreach (var sfx in SfxLibrary.All)
        {
            var samples = SfxLibrary.Build(sfx);
            Assert.True(MathF.Abs(samples[0]) < 0.05f, $"{sfx} starts with a click");
            Assert.True(MathF.Abs(samples[^1]) < 0.05f, $"{sfx} ends with a click");
        }
    }

    [Theory]
    [InlineData(MusicTrack.Title)]
    [InlineData(MusicTrack.District)]
    [InlineData(MusicTrack.Boss)]
    public void MusicLoopsAreLongAudibleAndBounded(MusicTrack track)
    {
        var samples = MusicLibrary.Build(track);

        Assert.True(samples.Length > SynthBuffer.SampleRate * 4, $"{track} loop is too short");
        Assert.All(samples, sample => Assert.InRange(sample, -1f, 1f));
        Assert.True(Peak(samples) > 0.2f);
        Assert.True(MathF.Abs(samples[0]) < 0.05f);
        Assert.True(MathF.Abs(samples[^1]) < 0.05f);
    }

    [Fact]
    public void DistrictLoopChangesWithTheLevelTheme()
    {
        var graveyard = MusicLibrary.Build(MusicTrack.District, LevelTheme.Graveyard);
        var mire = MusicLibrary.Build(MusicTrack.District, LevelTheme.Mire);

        Assert.Equal(graveyard.Length, mire.Length);
        Assert.NotEqual(graveyard, mire);
    }

    [Fact]
    public void TitlesAndBossLoopsAreDistinct()
    {
        var title = MusicLibrary.Build(MusicTrack.Title);
        var boss = MusicLibrary.Build(MusicTrack.Boss);

        Assert.NotEqual(title, boss);
    }

    [Fact]
    public void EveryLevelThemeMapsToADistinctRoot()
    {
        var roots = Enum.GetValues<LevelTheme>().Select(MusicLibrary.RootNote).ToArray();

        Assert.Equal(roots.Length, roots.Distinct().Count());
    }

    private static float Peak(float[] samples)
    {
        var peak = 0f;
        foreach (var sample in samples)
        {
            peak = MathF.Max(peak, MathF.Abs(sample));
        }

        return peak;
    }

    private static int CountZeroCrossings(float[] samples, int start, int count)
    {
        var crossings = 0;
        var previous = samples[start];
        for (var i = start + 1; i < start + count && i < samples.Length; i++)
        {
            if ((previous < 0f && samples[i] >= 0f) || (previous >= 0f && samples[i] < 0f))
            {
                crossings++;
            }

            previous = samples[i];
        }

        return crossings;
    }
}
