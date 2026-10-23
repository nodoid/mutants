using Mutants.Core;

namespace Mutants.Core.Tests;

public class LevelTests
{
    public static TheoryData<int> LevelIndices => new() { 0, 1, 2, 3, 4 };

    [Fact]
    public void Catalog_HasFiveLevels()
    {
        Assert.Equal(LevelCatalog.Count, LevelCatalog.Definitions.Count);
        Assert.Equal(5, LevelCatalog.LoadAll().Count);
    }

    [Theory]
    [MemberData(nameof(LevelIndices))]
    public void EveryLevel_ParsesWithExpectedFixtures(int index)
    {
        var map = LevelCatalog.Load(index);

        Assert.Equal(24, map.Columns);
        Assert.Equal(16, map.Rows);
        Assert.Equal(3, map.RequiredKeys);
        Assert.Equal(3, map.Pickups.Count(p => p.Kind == PickupKind.Key));
        Assert.NotEmpty(map.Enemies);
        Assert.Equal(TileKind.Gate, map[map.GateCell.Column, map.GateCell.Row]);
        Assert.Equal(TileKind.Portal, map[map.PortalCell.Column, map.PortalCell.Row]);
        Assert.Equal(TileKind.Floor, map[map.BossCell.Column, map.BossCell.Row]);
        Assert.Equal(TileKind.Floor, map[map.SpawnCell.Column, map.SpawnCell.Row]);
    }

    [Theory]
    [MemberData(nameof(LevelIndices))]
    public void Keys_AreReachableBeforeTheGateOpens(int index)
    {
        var map = LevelCatalog.Load(index);
        var reachable = map.FloodFill(gateOpen: false);

        foreach (var key in map.Pickups.Where(p => p.Kind == PickupKind.Key))
        {
            Assert.Contains(new Cell(key.Column, key.Row), reachable);
        }
    }

    [Theory]
    [MemberData(nameof(LevelIndices))]
    public void BossAndPortal_AreSealedUntilTheGateOpens(int index)
    {
        var map = LevelCatalog.Load(index);
        var sealedOff = map.FloodFill(gateOpen: false);
        var opened = map.FloodFill(gateOpen: true);

        Assert.DoesNotContain(map.BossCell, sealedOff);
        Assert.DoesNotContain(map.PortalCell, sealedOff);
        Assert.Contains(map.BossCell, opened);
        Assert.Contains(map.PortalCell, opened);
    }

    [Theory]
    [MemberData(nameof(LevelIndices))]
    public void EnemyAndPickupCells_AreStandable(int index)
    {
        var map = LevelCatalog.Load(index);

        foreach (var enemy in map.Enemies)
        {
            Assert.True(map.IsWalkable(enemy.Column, enemy.Row, gateOpen: true));
        }

        foreach (var pickup in map.Pickups)
        {
            Assert.True(map.IsWalkable(pickup.Column, pickup.Row, gateOpen: true));
        }

        Assert.False(map.IsBlocked(map.SpawnPosition, 22f, gateOpen: false));
    }

    [Fact]
    public void IsWalkable_GateDependsOnOpenState()
    {
        var map = LevelCatalog.Load(0);

        Assert.False(map.IsWalkable(map.GateCell.Column, map.GateCell.Row, gateOpen: false));
        Assert.True(map.IsWalkable(map.GateCell.Column, map.GateCell.Row, gateOpen: true));
    }

    [Fact]
    public void IsBlocked_StopsCircleAtWall()
    {
        var map = LevelCatalog.Load(0);

        // Wall runs along the top of the map.
        Assert.True(map.IsBlocked(new System.Numerics.Vector2(200f, 40f), 22f, gateOpen: true));
        Assert.False(map.IsBlocked(new System.Numerics.Vector2(200f, 200f), 22f, gateOpen: true));
    }

    [Fact]
    public void Parse_RejectsUnevenRows()
    {
        var definition = new LevelDefinition("Bad", "", LevelTheme.Graveyard, new[]
        {
            "#####",
            "#S.X#",
            "####",
        });

        Assert.Throws<ArgumentException>(() => LevelMap.Parse(definition));
    }

    [Fact]
    public void Parse_RejectsMissingFixtures()
    {
        var noSpawn = new LevelDefinition("Bad", "", LevelTheme.Graveyard, new[]
        {
            "#####",
            "#..X#",
            "#.DP#",
            "#####",
        });

        Assert.Throws<FormatException>(() => LevelMap.Parse(noSpawn));

        var unknown = new LevelDefinition("Bad", "", LevelTheme.Graveyard, new[]
        {
            "#####",
            "#S.X#",
            "#?DP#",
            "#####",
        });

        Assert.Throws<FormatException>(() => LevelMap.Parse(unknown));
    }

    [Fact]
    public void Parse_RejectsOpenBorder()
    {
        var open = new LevelDefinition("Bad", "", LevelTheme.Graveyard, new[]
        {
            "#####",
            "#S.X#",
            "#.DP#",
            "###.#",
        });

        Assert.Throws<FormatException>(() => LevelMap.Parse(open));
    }

    [Fact]
    public void FloodFill_RespectsWaterAndObstacles()
    {
        var map = LevelCatalog.Load(1);

        // Water at column 4, rows 10..14, walls the player off from those cells
        // unless they walk around; the tiles themselves are not walkable.
        Assert.False(map.IsWalkable(4, 10, gateOpen: true));
        Assert.False(map.IsWalkable(4, 14, gateOpen: true));
    }
}

public class DifficultyTests
{
    [Fact]
    public void AllDifficulties_ArePresentAndDistinct()
    {
        var profiles = DifficultyProfile.All;

        Assert.Equal(3, profiles.Count);
        Assert.Equal(profiles.Select(p => p.Difficulty).Distinct().Count(), profiles.Count);
        Assert.All(profiles, p => Assert.True(p.StartingLives > 0));
        Assert.All(profiles, p => Assert.True(p.BossHealth > 0));
        Assert.All(profiles, p => Assert.True(p.ScoreMultiplier > 0));
    }

    [Fact]
    public void HigherDifficulties_AreStricter()
    {
        var easy = DifficultyProfile.For(Difficulty.Dusk);
        var normal = DifficultyProfile.For(Difficulty.Midnight);
        var hard = DifficultyProfile.For(Difficulty.EternalNight);

        Assert.True(easy.StartingLives > normal.StartingLives);
        Assert.True(normal.StartingLives > hard.StartingLives);
        Assert.True(easy.EnemySpeedMultiplier < normal.EnemySpeedMultiplier);
        Assert.True(normal.EnemySpeedMultiplier < hard.EnemySpeedMultiplier);
        Assert.True(easy.EnemyHealthMultiplier < normal.EnemyHealthMultiplier);
        Assert.True(normal.EnemyHealthMultiplier < hard.EnemyHealthMultiplier);
        Assert.True(easy.ScoreMultiplier < normal.ScoreMultiplier);
        Assert.True(normal.ScoreMultiplier < hard.ScoreMultiplier);
        Assert.True(easy.BossHealth < normal.BossHealth);
        Assert.True(normal.BossHealth < hard.BossHealth);
        Assert.True(easy.HealBetweenLevels > hard.HealBetweenLevels);
    }

    [Fact]
    public void DifficultyDescription_IsReadable()
    {
        foreach (var profile in DifficultyProfile.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(profile.Name));
            Assert.False(string.IsNullOrWhiteSpace(profile.Tagline));
            Assert.Equal(profile.Name, profile.Name.ToUpperInvariant());
        }
    }

    [Fact]
    public void For_RejectsUnknownDifficulty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DifficultyProfile.For((Difficulty)99));
    }
}
