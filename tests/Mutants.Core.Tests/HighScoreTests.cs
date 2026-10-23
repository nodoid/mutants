using Mutants.Core;

namespace Mutants.Core.Tests;

public class HighScoreTests
{
    private static ScoreEntry Entry(string initials, int score, int level = 1, int minutes = 0)
        => new(initials, score, Difficulty.Midnight, level, DateTimeOffset.UnixEpoch.AddMinutes(minutes));

    [Fact]
    public void Add_KeepsOnlyTheTopEight()
    {
        var table = new HighScoreTable();

        for (var i = 1; i <= 12; i++)
        {
            table.Add(Entry("AAA", i * 100), out _);
        }

        Assert.Equal(HighScoreTable.Capacity, table.Count);
        Assert.Equal(new[] { 1200, 1100, 1000, 900, 800, 700, 600, 500 },
            table.Entries.Select(e => e.Score));
    }

    [Fact]
    public void Add_SortsDescending()
    {
        var table = new HighScoreTable();

        table.Add(Entry("LOW", 100), out _);
        table.Add(Entry("TOP", 900), out _);
        table.Add(Entry("MID", 500), out _);

        Assert.Equal(new[] { 900, 500, 100 }, table.Entries.Select(e => e.Score));
    }

    [Fact]
    public void Add_RejectsScoreBelowFullBoard()
    {
        var table = Fill(topScore: 900);

        var added = table.Add(Entry("NEW", 50), out var rank);

        Assert.False(added);
        Assert.Equal(-1, rank);
        Assert.Equal(8, table.Count);
        Assert.DoesNotContain(table.Entries, e => e.Initials == "NEW");
    }

    [Fact]
    public void Add_AcceptsScoreThatBeatsLastPlace_AndEvictsIt()
    {
        var table = Fill(topScore: 900);

        var added = table.Add(Entry("WIN", 250), out var rank);

        Assert.True(added);
        Assert.Equal(7, rank);
        Assert.Equal(8, table.Count);
        Assert.Equal(250, table.Entries[^1].Score);
        Assert.Equal("WIN", table.Entries[^1].Initials);
        Assert.DoesNotContain(table.Entries, e => e.Score == 200);
    }

    [Fact]
    public void Qualifies_ReflectsCapacityAndLowestScore()
    {
        var table = new HighScoreTable();
        Assert.True(table.Qualifies(1));
        Assert.False(table.IsFull);
        Assert.Equal(-1, table.LowestScore);

        table = Fill(topScore: 900);

        Assert.True(table.Qualifies(201));
        Assert.False(table.Qualifies(200));
        Assert.False(table.Qualifies(0));
        Assert.Equal(200, table.LowestScore);
    }

    [Fact]
    public void Ties_AreBrokenByDepthThenEarliestTime()
    {
        var table = new HighScoreTable();
        table.Add(Entry("DEP", 500, level: 4, minutes: 5), out _);
        table.Add(Entry("EAR", 500, level: 2, minutes: 1), out _);
        table.Add(Entry("LTE", 500, level: 2, minutes: 9), out _);

        Assert.Equal(new[] { "DEP", "EAR", "LTE" }, table.Entries.Select(e => e.Initials));
    }

    [Fact]
    public void Constructor_OrdersLimitsAndNormalisesEntries()
    {
        var entries = Enumerable.Range(0, 10)
            .Select(i => new ScoreEntry("x!", (i + 1) * 10, Difficulty.Dusk, 9, DateTimeOffset.UnixEpoch));

        var table = new HighScoreTable(entries);

        Assert.Equal(8, table.Count);
        Assert.Equal(100, table.Entries[0].Score);
        Assert.Equal(30, table.Entries[^1].Score);
        Assert.All(table.Entries, e => Assert.True(Initials.IsValid(e.Initials)));
        Assert.All(table.Entries, e => Assert.InRange(e.LevelReached, 1, LevelCatalog.Count));
    }

    [Fact]
    public void Add_ClampsNegativeScoreToZero()
    {
        var table = new HighScoreTable();
        table.Add(new ScoreEntry("NEG", -500, Difficulty.Dusk, 1, DateTimeOffset.UnixEpoch), out _);

        Assert.Equal(0, table.Entries[0].Score);
    }

    [Theory]
    [InlineData(null, "AAA")]
    [InlineData("", "AAA")]
    [InlineData("!!!", "AAA")]
    [InlineData("a", "AAA")]
    [InlineData("ab", "ABA")]
    [InlineData("abc", "ABC")]
    [InlineData("abcd", "ABC")]
    [InlineData("a-b_c", "ABC")]
    [InlineData("7up", "7UP")]
    public void Sanitize_ProducesExactlyThreeAlphanumericCharacters(string? input, string expected)
    {
        var result = Initials.Sanitize(input);

        Assert.Equal(expected, result);
        Assert.True(Initials.IsValid(result));
    }

    [Theory]
    [InlineData("ABC", true)]
    [InlineData("A1C", true)]
    [InlineData("AB", false)]
    [InlineData("ABCD", false)]
    [InlineData("abC", false)]
    [InlineData("A C", false)]
    [InlineData(null, false)]
    public void IsValid_ChecksLengthAndAlphabet(string? value, bool expected)
    {
        Assert.Equal(expected, Initials.IsValid(value));
    }

    [Fact]
    public void ScoreEntry_Create_ClampsLevelAndKeepsDifficulty()
    {
        var entry = ScoreEntry.Create("q", 1234, Difficulty.EternalNight, levelReached: 99);

        Assert.Equal("QAA", entry.Initials);
        Assert.Equal(1234, entry.Score);
        Assert.Equal(Difficulty.EternalNight, entry.Difficulty);
        Assert.Equal(LevelCatalog.Count, entry.LevelReached);
        Assert.NotEqual(default, entry.AchievedUtc);
    }

    private static HighScoreTable Fill(int topScore)
    {
        var table = new HighScoreTable();
        for (var i = 0; i < HighScoreTable.Capacity; i++)
        {
            table.Add(Entry("OLD", topScore - (i * 100)), out _);
        }

        return table;
    }
}
