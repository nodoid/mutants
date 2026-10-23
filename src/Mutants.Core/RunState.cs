namespace Mutants.Core;

public enum RunOutcome
{
    InProgress,
    Defeat,
    Victory,
}

/// <summary>
/// A whole play-through: lives, level progression, banked score and final outcome.
/// The run never blocks on presentation; the UI reads its state to draw overlays.
/// </summary>
public sealed class RunState
{
    private readonly IReadOnlyList<LevelDefinition> _levels;

    public RunState(DifficultyProfile difficulty, IReadOnlyList<LevelDefinition>? levels = null)
    {
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        _levels = levels ?? LevelCatalog.Definitions;
        if (_levels.Count == 0)
        {
            throw new ArgumentException("A run needs at least one level.", nameof(levels));
        }

        Lives = difficulty.StartingLives;
        Health = PlayerState.MaxHealth;
        LevelIndex = 0;
        World = CreateWorld(0, Health);
    }

    public DifficultyProfile Difficulty { get; }

    public GameWorld World { get; private set; }

    public int LevelIndex { get; private set; }

    public int LevelNumber => LevelIndex + 1;

    public int LevelsCleared { get; private set; }

    public int Score { get; internal set; }

    public int Lives { get; private set; }

    public int Health { get; private set; }

    public RunOutcome Outcome { get; private set; } = RunOutcome.InProgress;

    public bool IsFinished => Outcome != RunOutcome.InProgress;

    public int TotalScore => Score + (World?.Score ?? 0);

    public void Update(float dt, GameInput input)
    {
        if (IsFinished || dt <= 0f)
        {
            return;
        }

        World.Update(dt, input);

        if (World.Status == WorldStatus.Cleared)
        {
            RegisterLevelCleared(World.Score);
        }
        else if (World.Status == WorldStatus.Failed)
        {
            RegisterPlayerDeath(World.Score);
        }
    }

    /// <summary>Banks a cleared level, heals per difficulty, and advances or wins.</summary>
    public void RegisterLevelCleared(int levelScore)
    {
        if (IsFinished)
        {
            return;
        }

        Score += Math.Max(0, levelScore);
        LevelsCleared++;
        Health = Math.Clamp(World.Player.Health + Difficulty.HealBetweenLevels, 1, PlayerState.MaxHealth);

        if (LevelIndex + 1 >= _levels.Count)
        {
            Outcome = RunOutcome.Victory;
            return;
        }

        LevelIndex++;
        World = CreateWorld(LevelIndex, Health);
    }

    /// <summary>Banks the level's score and spends a life, or ends the run.</summary>
    public void RegisterPlayerDeath(int levelScore)
    {
        if (IsFinished)
        {
            return;
        }

        Score += Math.Max(0, levelScore);
        Lives--;

        if (Lives <= 0)
        {
            Lives = 0;
            Outcome = RunOutcome.Defeat;
            return;
        }

        Health = PlayerState.MaxHealth;
        World = CreateWorld(LevelIndex, Health);
    }

    private GameWorld CreateWorld(int levelIndex, int health)
        => new(LevelMap.Parse(_levels[levelIndex]), Difficulty, health);
}
