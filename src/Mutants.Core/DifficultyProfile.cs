namespace Mutants.Core;

/// <summary>Selectable challenge levels. Ordered from gentlest to cruellest.</summary>
public enum Difficulty
{
    Dusk = 0,
    Midnight = 1,
    EternalNight = 2,
}

/// <summary>
/// Immutable tuning values for a difficulty. All gameplay advantages flow through
/// this record so the balance can be reasoned about (and tested) in one place.
/// </summary>
public sealed record DifficultyProfile(
    Difficulty Difficulty,
    string Name,
    string Tagline,
    int StartingLives,
    float EnemySpeedMultiplier,
    float EnemyHealthMultiplier,
    int ContactDamage,
    float ScoreMultiplier,
    int HealBetweenLevels,
    int BossHealth,
    int ExtraEnemies)
{
    public static DifficultyProfile For(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Dusk => new(
            Difficulty.Dusk,
            "DUSK",
            "The dead are drowsy. Five lives, softer blows.",
            StartingLives: 5,
            EnemySpeedMultiplier: 0.78f,
            EnemyHealthMultiplier: 0.75f,
            ContactDamage: 1,
            ScoreMultiplier: 1.0f,
            HealBetweenLevels: 3,
            BossHealth: 26,
            ExtraEnemies: 0),
        Difficulty.Midnight => new(
            Difficulty.Midnight,
            "MIDNIGHT",
            "The hour the graves open. Three lives, fair odds.",
            StartingLives: 3,
            EnemySpeedMultiplier: 1.0f,
            EnemyHealthMultiplier: 1.0f,
            ContactDamage: 1,
            ScoreMultiplier: 1.5f,
            HealBetweenLevels: 1,
            BossHealth: 38,
            ExtraEnemies: 0),
        Difficulty.EternalNight => new(
            Difficulty.EternalNight,
            "ETERNAL NIGHT",
            "Dawn never comes. Two lives, hungry horrors.",
            StartingLives: 2,
            EnemySpeedMultiplier: 1.28f,
            EnemyHealthMultiplier: 1.5f,
            ContactDamage: 2,
            ScoreMultiplier: 2.25f,
            HealBetweenLevels: 0,
            BossHealth: 56,
            ExtraEnemies: 3),
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown difficulty."),
    };

    public static IReadOnlyList<DifficultyProfile> All { get; } = new[]
    {
        For(Difficulty.Dusk),
        For(Difficulty.Midnight),
        For(Difficulty.EternalNight),
    };

    public int StartingHealth { get; } = 6;
}
