using Mutants.Core;

namespace Mutants.Core.Tests;

public sealed class HighScoreStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _path;
    private readonly FileHighScoreStore _store;

    public HighScoreStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "mutants-tests-" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "highscores.json");
        _store = new FileHighScoreStore(_path);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsEmptyBoard()
    {
        var table = _store.Load();

        Assert.Empty(table.Entries);
        Assert.False(File.Exists(_path));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsTopEight()
    {
        var table = new HighScoreTable();
        for (var i = 0; i < 10; i++)
        {
            table.Add(ScoreEntry.Create(
                initials: "PL" + i,
                score: 1000 + i,
                difficulty: i % 2 == 0 ? Difficulty.Dusk : Difficulty.EternalNight,
                levelReached: (i % 5) + 1,
                achievedUtc: DateTimeOffset.UnixEpoch.AddHours(i)), out _);
        }

        _store.Save(table);
        var loaded = _store.Load();

        Assert.Equal(8, loaded.Count);
        Assert.Equal(table.Entries.Select(e => e.Score), loaded.Entries.Select(e => e.Score));
        Assert.Equal(table.Entries.Select(e => e.Initials), loaded.Entries.Select(e => e.Initials));
        Assert.Equal(Difficulty.EternalNight, loaded.Entries[0].Difficulty);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        Assert.False(Directory.Exists(_directory));

        _store.Save(new HighScoreTable());

        Assert.True(File.Exists(_path));
    }

    [Fact]
    public void Load_CorruptJson_ReturnsEmptyBoardInsteadOfThrowing()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "{ this is not json ]");

        var table = _store.Load();

        Assert.Empty(table.Entries);
    }

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyBoard()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, string.Empty);

        Assert.Empty(_store.Load().Entries);
    }

    [Fact]
    public void Load_IgnoresInvalidEntriesAndKeepsBestEight()
    {
        Directory.CreateDirectory(_directory);
        var scores = string.Join(',', Enumerable.Range(0, 12).Select(i =>
            $"{{\"initials\":\"P{i}\",\"score\":{i * 100},\"difficulty\":\"Midnight\",\"levelReached\":1,\"achievedUtc\":\"2024-01-01T00:00:00+00:00\"}}"));
        File.WriteAllText(_path, $"{{\"version\":1,\"scores\":[{scores}]}}");

        var table = _store.Load();

        Assert.Equal(8, table.Count);
        Assert.Equal(1100, table.Entries[0].Score);
        Assert.Equal(400, table.Entries[^1].Score);
    }

    [Fact]
    public void Save_IsAtomicAndOverwritesPreviousBoard()
    {
        var first = new HighScoreTable(new[]
        {
            ScoreEntry.Create("OLD", 100, Difficulty.Dusk, 1),
        });
        _store.Save(first);

        var second = new HighScoreTable(new[]
        {
            ScoreEntry.Create("NEW", 900, Difficulty.Midnight, 3),
        });
        _store.Save(second);
        var loaded = _store.Load();

        Assert.Single(loaded.Entries);
        Assert.Equal("NEW", loaded.Entries[0].Initials);
        Assert.Equal(900, loaded.Entries[0].Score);
        Assert.Equal(3, loaded.Entries[0].LevelReached);
    }

    [Fact]
    public void DefaultPath_IsInsideLocalApplicationData()
    {
        var expectedRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(expectedRoot, FileHighScoreStore.DefaultFilePath);
        Assert.EndsWith("highscores.json", FileHighScoreStore.DefaultFilePath);
    }
}
