using Mutants.Core;

namespace Mutants.Core.Tests;

public class RunStateTests
{
    private static RunState NewRun(Difficulty difficulty = Difficulty.Midnight)
        => new(DifficultyProfile.For(difficulty));

    [Fact]
    public void NewRun_StartsAtFirstLevelWithDifficultyLives()
    {
        var run = NewRun(Difficulty.Midnight);

        Assert.Equal(RunOutcome.InProgress, run.Outcome);
        Assert.Equal(0, run.LevelIndex);
        Assert.Equal(1, run.LevelNumber);
        Assert.Equal(3, run.Lives);
        Assert.Equal(PlayerState.MaxHealth, run.Health);
        Assert.Equal(0, run.TotalScore);
        Assert.Equal("Hollow Hill", run.World.Map.Name);
    }

    [Fact]
    public void StartingLives_FollowDifficulty()
    {
        Assert.Equal(5, NewRun(Difficulty.Dusk).Lives);
        Assert.Equal(3, NewRun(Difficulty.Midnight).Lives);
        Assert.Equal(2, NewRun(Difficulty.EternalNight).Lives);
    }

    [Fact]
    public void ClearingLevel_BanksScore_AdvancesAndHeals()
    {
        var run = NewRun(Difficulty.Dusk);
        run.World.Player.Health = 2;

        run.RegisterLevelCleared(1234);

        Assert.Equal(1234, run.Score);
        Assert.Equal(1, run.LevelsCleared);
        Assert.Equal(1, run.LevelIndex);
        Assert.Equal("Sunken Mire", run.World.Map.Name);
        Assert.Equal(Math.Min(PlayerState.MaxHealth, 2 + DifficultyProfile.For(Difficulty.Dusk).HealBetweenLevels), run.Health);
        Assert.Equal(run.Health, run.World.Player.Health);
    }

    [Fact]
    public void ClearingAllLevels_EndsInVictory()
    {
        var run = NewRun();
        var scores = new[] { 100, 200, 300, 400, 500 };

        foreach (var score in scores)
        {
            run.RegisterLevelCleared(score);
        }

        Assert.Equal(RunOutcome.Victory, run.Outcome);
        Assert.True(run.IsFinished);
        Assert.Equal(5, run.LevelsCleared);
        Assert.Equal(1500, run.Score);
        Assert.Equal(4, run.LevelIndex);
        Assert.Equal("The Gears of Night", run.World.Map.Name);
    }

    [Fact]
    public void Victory_ScoresCannotBeBankedTwice()
    {
        var run = NewRun();
        for (var i = 0; i < 5; i++)
        {
            run.RegisterLevelCleared(100);
        }

        run.RegisterLevelCleared(9999);
        run.RegisterPlayerDeath(9999);

        Assert.Equal(500, run.Score);
        Assert.Equal(RunOutcome.Victory, run.Outcome);
    }

    [Fact]
    public void Death_SpendsALife_AndRestartsSameLevelAtFullHealth()
    {
        var run = NewRun();
        run.World.Player.Health = 1;
        run.Score = 0;

        run.RegisterPlayerDeath(400);

        Assert.Equal(2, run.Lives);
        Assert.Equal(400, run.Score);
        Assert.Equal(RunOutcome.InProgress, run.Outcome);
        Assert.Equal(0, run.LevelIndex);
        Assert.Equal(PlayerState.MaxHealth, run.Health);
        Assert.Equal(PlayerState.MaxHealth, run.World.Player.Health);
    }

    [Fact]
    public void FinalDeath_EndsInDefeat_WithZeroLives()
    {
        var run = NewRun(Difficulty.EternalNight);
        Assert.Equal(2, run.Lives);

        run.RegisterPlayerDeath(100);
        Assert.Equal(1, run.Lives);

        run.RegisterPlayerDeath(200);

        Assert.Equal(RunOutcome.Defeat, run.Outcome);
        Assert.True(run.IsFinished);
        Assert.Equal(0, run.Lives);
        Assert.Equal(300, run.Score);
    }

    [Fact]
    public void DeathsCannotBeRegisteredAfterRunEnds()
    {
        var run = NewRun(Difficulty.EternalNight);
        run.RegisterPlayerDeath(100);
        run.RegisterPlayerDeath(200);

        run.RegisterPlayerDeath(300);

        Assert.Equal(300, run.Score);
        Assert.Equal(RunOutcome.Defeat, run.Outcome);
    }

    [Fact]
    public void TotalScore_IncludesUnbankedLevelScore()
    {
        var run = NewRun(Difficulty.Dusk);
        run.Score = 500;
        var coin = run.World.Pickups.First(p => p.Kind == PickupKind.Coin);
        run.World.Player.Position = coin.Position;
        run.Update(1f / 60f, GameInput.None);

        Assert.True(run.TotalScore > 500);
        Assert.Equal(500, run.Score);
    }

    [Fact]
    public void Update_StopsAfterRunFinishes()
    {
        var run = NewRun(Difficulty.EternalNight);
        run.RegisterPlayerDeath(0);
        run.RegisterPlayerDeath(0);

        var worldBefore = run.World;
        run.Update(1f, GameInput.None);

        Assert.Same(worldBefore, run.World);
        Assert.Equal(RunOutcome.Defeat, run.Outcome);
    }

    [Fact]
    public void LevelClearThenDeathOnLastLife_EndsWithBankedScores()
    {
        var run = NewRun(Difficulty.EternalNight);
        run.RegisterLevelCleared(250);

        run.RegisterPlayerDeath(75);
        run.RegisterPlayerDeath(25);

        Assert.Equal(350, run.Score);
        Assert.Equal(1, run.LevelsCleared);
        Assert.Equal(RunOutcome.Defeat, run.Outcome);
    }
}
