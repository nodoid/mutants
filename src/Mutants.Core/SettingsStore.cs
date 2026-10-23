using System.Diagnostics;
using System.Text.Json;

namespace Mutants.Core;

public sealed record GameSettings(bool SoundEnabled = true)
{
    public static GameSettings Default { get; } = new();
}

public interface ISettingsStore
{
    GameSettings Load();

    void Save(GameSettings settings);
}

/// <summary>
/// Small durable settings file next to the score board. Bad files fall back to
/// defaults so a damaged preference never stops the game from starting.
/// </summary>
public sealed class FileSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly object _gate = new();

    public FileSettingsStore(string? filePath = null)
    {
        FilePath = filePath ?? DefaultFilePath;
    }

    public string FilePath { get; }

    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Mutants",
        "settings.json");

    public GameSettings Load()
    {
        lock (_gate)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return GameSettings.Default;
                }

                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return GameSettings.Default;
                }

                var document = JsonSerializer.Deserialize<SettingsDocument>(json, JsonOptions);
                return document is null
                    ? GameSettings.Default
                    : new GameSettings(document.SoundEnabled);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                Debug.WriteLine($"Could not read settings from {FilePath}: {ex}");
                return GameSettings.Default;
            }
        }
    }

    public void Save(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_gate)
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var document = new SettingsDocument
            {
                Version = 1,
                SoundEnabled = settings.SoundEnabled,
            };

            var json = JsonSerializer.Serialize(document, JsonOptions);
            var tempPath = FilePath + ".tmp";

            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, overwrite: true);
        }
    }

    private sealed class SettingsDocument
    {
        public int Version { get; set; } = 1;

        public bool SoundEnabled { get; set; } = true;
    }
}
