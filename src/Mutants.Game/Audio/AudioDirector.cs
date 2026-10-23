using Microsoft.Xna.Framework.Audio;
using Mutants.Core;
using Mutants.Core.Audio;

namespace Mutants.Game.Audio;

/// <summary>
/// Turns the procedural buffers from <see cref="SfxLibrary"/> and
/// <see cref="MusicLibrary"/> into MonoGame sounds, then plays them. All audio is
/// generated on the device, so there are no content-pipeline assets to load.
/// </summary>
public sealed class AudioDirector : IDisposable
{
    private readonly Dictionary<Sfx, SoundEffect> _effects = new();
    private readonly Dictionary<(MusicTrack Track, LevelTheme Theme), SoundEffect> _music = new();
    private readonly Random _random = new(2411);

    private SoundEffectInstance? _musicInstance;
    private MusicTrack _track = MusicTrack.None;
    private LevelTheme _theme = LevelTheme.Graveyard;
    private bool _muted;

    public AudioDirector()
    {
        foreach (var sfx in SfxLibrary.All)
        {
            _effects[sfx] = ToSoundEffect(SfxLibrary.Build(sfx));
        }

        // The title loop is needed on the first frame; render it up front so the
        // menu never stutters while the synth catches up.
        GetMusic(MusicTrack.Title, LevelTheme.Graveyard);
    }

    public bool Muted
    {
        get => _muted;
        set
        {
            if (_muted == value)
            {
                return;
            }

            _muted = value;
            if (_muted)
            {
                StopMusic();
            }
        }
    }

    public float EffectVolume { get; set; } = 1f;

    public float MusicVolume { get; set; } = 0.55f;

    public void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0f)
    {
        if (_muted || volume <= 0f || !_effects.TryGetValue(sfx, out var effect))
        {
            return;
        }

        var pitch = pitchJitter > 0f
            ? (float)(((_random.NextDouble() * 2.0) - 1.0) * pitchJitter)
            : 0f;

        try
        {
            effect.Play(Math.Clamp(volume * EffectVolume, 0f, 1f), Math.Clamp(pitch, -1f, 1f), 0f);
        }
        catch (InstancePlayLimitException)
        {
            // Too many voices in flight; dropping a shot blip is fine.
        }
    }

    /// <summary>Starts a loop if it is not already the current one.</summary>
    public void EnsureMusic(MusicTrack track, LevelTheme theme = LevelTheme.Graveyard)
    {
        if (_muted || track == MusicTrack.None)
        {
            StopMusic();
            return;
        }

        if (track == _track && theme == _theme && _musicInstance is not null)
        {
            return;
        }

        StopMusic();

        _musicInstance = GetMusic(track, theme).CreateInstance();
        _musicInstance.IsLooped = true;
        _musicInstance.Volume = MusicVolume;
        _musicInstance.Play();
        _track = track;
        _theme = theme;
    }

    public void StopMusic()
    {
        if (_musicInstance is null)
        {
            return;
        }

        _musicInstance.Stop();
        _musicInstance.Dispose();
        _musicInstance = null;
        _track = MusicTrack.None;
    }

    public void PauseMusic() => _musicInstance?.Pause();

    public void ResumeMusic()
    {
        if (_muted)
        {
            return;
        }

        if (_musicInstance?.State == SoundState.Paused)
        {
            _musicInstance.Resume();
        }
    }

    public void Dispose()
    {
        StopMusic();

        foreach (var effect in _effects.Values)
        {
            effect.Dispose();
        }

        foreach (var effect in _music.Values)
        {
            effect.Dispose();
        }

        _effects.Clear();
        _music.Clear();
    }

    private SoundEffect GetMusic(MusicTrack track, LevelTheme theme)
    {
        if (_music.TryGetValue((track, theme), out var cached))
        {
            return cached;
        }

        var effect = ToSoundEffect(MusicLibrary.Build(track, theme));
        _music[(track, theme)] = effect;
        return effect;
    }

    private static SoundEffect ToSoundEffect(float[] samples)
    {
        var pcm = new byte[samples.Length * sizeof(short)];
        for (var i = 0; i < samples.Length; i++)
        {
            var value = (short)Math.Clamp(
                (int)MathF.Round(samples[i] * short.MaxValue),
                short.MinValue,
                short.MaxValue);

            pcm[i * 2] = (byte)(value & 0xFF);
            pcm[(i * 2) + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new SoundEffect(pcm, SynthBuffer.SampleRate, AudioChannels.Mono);
    }
}
