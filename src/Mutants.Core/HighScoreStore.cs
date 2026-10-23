using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mutants.Core;

public interface IHighScoreStore
{
    HighScoreTable Load();

    void Save(HighScoreTable table);
}

/// <summary>
/// Durable, crash-tolerant score board. Scores live in the app's local data folder
/// and survive relaunches. Writes are atomic (temp file + move).
/// </summary>
public sealed class FileHighScoreStore : IHighScoreStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly object _gate = new();

    public FileHighScoreStore(string? filePath = null)
    {
        FilePath = filePath ?? DefaultFilePath;
    }

    public string FilePath { get; }

    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Mutants",
        "highscores.json");

    public HighScoreTable Load()
    {
        lock (_gate)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new HighScoreTable();
                }

                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new HighScoreTable();
                }

                var document = JsonSerializer.Deserialize<ScoreDocument>(json, JsonOptions);
                if (document?.Scores is null)
                {
                    return new HighScoreTable();
                }

                var valid = document.Scores
                    .Where(e => e is not null && e.Score >= 0)
                    .Select(e => e!);

                return new HighScoreTable(valid);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                // A damaged or unreadable board must never prevent the game from starting.
                Debug.WriteLine($"Could not read high scores from {FilePath}: {ex}");
                return new HighScoreTable();
            }
        }
    }

    public void Save(HighScoreTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        lock (_gate)
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var document = new ScoreDocument
            {
                Version = 1,
                Scores = table.Entries.Take(HighScoreTable.Capacity).ToList(),
            };

            var json = JsonSerializer.Serialize(document, JsonOptions);
            var tempPath = FilePath + ".tmp";

            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, overwrite: true);
        }
    }

    private sealed class ScoreDocument
    {
        public int Version { get; set; } = 1;

        public List<ScoreEntry> Scores { get; set; } = new();
    }
}
