using Mutants.Core;

namespace Mutants.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _path;
    private readonly FileSettingsStore _store;

    public SettingsStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "mutants-settings-" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "settings.json");
        _store = new FileSettingsStore(_path);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_EnablesSound()
    {
        var settings = _store.Load();

        Assert.True(settings.SoundEnabled);
        Assert.False(File.Exists(_path));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsTheMutedPreference()
    {
        _store.Save(new GameSettings(SoundEnabled: false));

        Assert.False(_store.Load().SoundEnabled);
    }

    [Fact]
    public void Save_EnablesSoundAgain()
    {
        _store.Save(new GameSettings(SoundEnabled: false));
        _store.Save(new GameSettings(SoundEnabled: true));

        Assert.True(_store.Load().SoundEnabled);
    }

    [Fact]
    public void Save_WritesAtomicallyAndLeavesNoTemporaryFile()
    {
        _store.Save(new GameSettings(SoundEnabled: false));

        Assert.True(File.Exists(_path));
        Assert.False(File.Exists(_path + ".tmp"));
        Assert.Contains("soundEnabled", File.ReadAllText(_path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{\"version\":1,\"soundEnabled\":")]
    public void Load_WithDamagedFile_FallsBackToSoundOn(string contents)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, contents);

        Assert.True(_store.Load().SoundEnabled);
    }
}
