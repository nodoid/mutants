namespace Mutants.Core;

/// <summary>Top-score entry. Initials are always three alphanumeric characters.</summary>
public sealed record ScoreEntry(
    string Initials,
    int Score,
    Difficulty Difficulty,
    int LevelReached,
    DateTimeOffset AchievedUtc)
{
    public static ScoreEntry Create(
        string? initials,
        int score,
        Difficulty difficulty,
        int levelReached,
        DateTimeOffset? achievedUtc = null)
        => new(
            Core.Initials.Sanitize(initials),
            Math.Max(0, score),
            Enum.IsDefined(difficulty) ? difficulty : Difficulty.Midnight,
            Math.Clamp(levelReached, 1, LevelCatalog.Count),
            achievedUtc ?? DateTimeOffset.UtcNow);
}

/// <summary>Keeps only the best eight runs. Ties are broken by depth, then earliest time.</summary>
public sealed class HighScoreTable
{
    public const int Capacity = 8;

    private readonly List<ScoreEntry> _entries;

    public HighScoreTable(IEnumerable<ScoreEntry>? entries = null)
    {
        _entries = (entries ?? Array.Empty<ScoreEntry>())
            .Where(e => e is not null)
            .Select(Normalize)
            .OrderByDescending(e => e.Score)
            .ThenByDescending(e => e.LevelReached)
            .ThenBy(e => e.AchievedUtc)
            .Take(Capacity)
            .ToList();
    }

    public IReadOnlyList<ScoreEntry> Entries => _entries;

    public int Count => _entries.Count;

    public bool IsFull => Count >= Capacity;

    /// <summary>Lowest stored score, or -1 when the board still has room.</summary>
    public int LowestScore => IsFull ? _entries[^1].Score : -1;

    public bool Qualifies(int score) => !IsFull || score > _entries[^1].Score;

    /// <summary>Inserts a score when it earns a place. Returns true if the entry was stored.</summary>
    public bool Add(ScoreEntry entry, out int rank)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var normalised = Normalize(entry);

        _entries.Add(normalised);
        _entries.Sort(Compare);

        rank = _entries.IndexOf(normalised);
        if (_entries.Count > Capacity)
        {
            _entries.RemoveRange(Capacity, _entries.Count - Capacity);
        }

        if (rank >= Capacity)
        {
            rank = -1;
            return false;
        }

        return true;
    }

    private static ScoreEntry Normalize(ScoreEntry entry)
        => entry with
        {
            Initials = Core.Initials.Sanitize(entry.Initials),
            Score = Math.Max(0, entry.Score),
            LevelReached = Math.Clamp(entry.LevelReached, 1, LevelCatalog.Count),
        };

    private static int Compare(ScoreEntry a, ScoreEntry b)
    {
        var byScore = b.Score.CompareTo(a.Score);
        if (byScore != 0)
        {
            return byScore;
        }

        var byDepth = b.LevelReached.CompareTo(a.LevelReached);
        if (byDepth != 0)
        {
            return byDepth;
        }

        return a.AchievedUtc.CompareTo(b.AchievedUtc);
    }
}

/// <summary>Normalises player initials for the score board.</summary>
public static class Initials
{
    public const int Length = 3;

    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "AAA";
        }

        var cleaned = new string(input
            .ToUpperInvariant()
            .Where(ch => ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
            .Take(Length)
            .ToArray());

        if (cleaned.Length == 0)
        {
            return "AAA";
        }

        return cleaned.PadRight(Length, 'A');
    }

    public static bool IsValid(string? initials)
        => initials is { Length: Length } && initials.All(ch => ch is >= 'A' and <= 'Z' or >= '0' and <= '9');
}
