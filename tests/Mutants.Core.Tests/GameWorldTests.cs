using System.Numerics;
using Mutants.Core;

namespace Mutants.Core.Tests;

public class GameWorldTests
{
    private static GameWorld NewWorld(
        Difficulty difficulty = Difficulty.Midnight,
        int level = 0,
        int health = PlayerState.MaxHealth)
        => new(LevelCatalog.Load(level), DifficultyProfile.For(difficulty), health);

    private static void Step(GameWorld world, Vector2 move, bool attack = false, int frames = 1)
    {
        for (var i = 0; i < frames; i++)
        {
            world.Update(1f / 60f, new GameInput(move, attack));
        }
    }

    private static HashSet<WorldEventType> StepEvents(GameWorld world, Vector2 move, bool attack = false, int frames = 1)
    {
        var seen = new HashSet<WorldEventType>();
        for (var i = 0; i < frames; i++)
        {
            world.Update(1f / 60f, new GameInput(move, attack));
            foreach (var evt in world.Events)
            {
                seen.Add(evt.Type);
            }
        }

        return seen;
    }

    [Fact]
    public void World_StartsAtSpawnWithFullHealth()
    {
        var world = NewWorld();

        Assert.Equal(WorldStatus.Playing, world.Status);
        Assert.Equal(PlayerState.MaxHealth, world.Player.Health);
        Assert.Equal(world.Map.SpawnPosition, world.Player.Position);
        Assert.Equal(0, world.Score);
        Assert.False(world.GateOpen);
        Assert.Equal(3, world.RequiredKeys);
        Assert.Equal(0, world.KeysCollected);
        Assert.False(world.PortalActive);
    }

    [Fact]
    public void Player_CannotWalkThroughOuterWalls()
    {
        var world = NewWorld();

        Step(world, new Vector2(-1f, -1f), frames: 120);

        Assert.True(world.Player.Position.X >= world.Player.Radius);
        Assert.True(world.Player.Position.Y >= world.Player.Radius);
        Assert.False(world.Map.IsBlocked(world.Player.Position, world.Player.Radius, gateOpen: true));
    }

    [Fact]
    public void Player_CannotCrossClosedGate()
    {
        var world = NewWorld();
        var gate = world.Map.GatePosition;

        world.Player.Position = gate - new Vector2(180f, 0f);
        Step(world, Vector2.UnitX, frames: 180);

        Assert.True(world.Player.Position.X < gate.X - 20f);
        Assert.False(world.GateOpen);
    }

    [Fact]
    public void CollectingEveryKey_OpensTheGate()
    {
        var world = NewWorld();
        var keys = world.Pickups.Where(p => p.Kind == PickupKind.Key).ToList();

        Assert.Equal(3, keys.Count);

        foreach (var key in keys)
        {
            world.Player.Position = key.Position;
            Step(world, Vector2.Zero);
        }

        Assert.Equal(3, world.KeysCollected);
        Assert.True(world.GateOpen);
        Assert.Contains(world.Events, e => e.Type == WorldEventType.GateOpened);
        Assert.Equal(3, world.Pickups.Count(p => p.Kind == PickupKind.Key && p.Collected));
    }

    [Fact]
    public void Hearts_OnlyPickUpWhenHurt()
    {
        var world = NewWorld(health: PlayerState.MaxHealth);
        var heart = world.Pickups.First(p => p.Kind == PickupKind.Heart);

        world.Player.Position = heart.Position;
        Step(world, Vector2.Zero);
        Assert.False(heart.Collected);

        world.Player.Health = 2;
        Step(world, Vector2.Zero);

        Assert.True(heart.Collected);
        Assert.Equal(4, world.Player.Health);
    }

    [Fact]
    public void Coins_ScoreByDifficulty()
    {
        var easy = NewWorld(Difficulty.Dusk);
        var hard = NewWorld(Difficulty.EternalNight);

        var easyCoin = easy.Pickups.First(p => p.Kind == PickupKind.Coin);
        var hardCoin = hard.Pickups.First(p => p.Kind == PickupKind.Coin);

        easy.Player.Position = easyCoin.Position;
        Step(easy, Vector2.Zero);

        hard.Player.Position = hardCoin.Position;
        Step(hard, Vector2.Zero);

        Assert.True(easy.Score > 0);
        Assert.True(hard.Score > easy.Score);
        Assert.Contains(easy.Events, e => e.Type == WorldEventType.Coin);
    }

    [Fact]
    public void Projectiles_DamageAndKillEnemies_AndAwardScore()
    {
        var world = NewWorld();
        var enemy = world.Enemies.First(e => e.Kind == EnemyKind.Crawler);
        enemy.Health = 2;

        world.Player.Position = enemy.Position - new Vector2(110f, 0f);
        var events = StepEvents(world, Vector2.Zero, attack: true, frames: 20);

        Assert.False(enemy.IsAlive);
        Assert.True(world.Score > 0);
        Assert.Contains(WorldEventType.EnemyKilled, events);
    }

    [Fact]
    public void EnemyContact_HurtsOnceThenInvulnerabilityProtects()
    {
        var world = NewWorld();
        var enemy = world.Enemies.First();

        world.Player.Position = enemy.Position;
        Step(world, Vector2.Zero);

        Assert.Equal(PlayerState.MaxHealth - 1, world.Player.Health);
        Assert.True(world.Player.InvulnerableTime > 0f);

        // Keep standing on the enemy during the invulnerability window.
        for (var i = 0; i < 10; i++)
        {
            world.Player.Position = enemy.Position;
            world.Player.InvulnerableTime = 0.5f;
            Step(world, Vector2.Zero);
        }

        Assert.Equal(PlayerState.MaxHealth - 1, world.Player.Health);
    }

    [Fact]
    public void HardDifficulty_DealsMoreContactDamage()
    {
        var easyWorld = NewWorld(Difficulty.Dusk);
        var hardWorld = NewWorld(Difficulty.EternalNight);

        easyWorld.Player.Position = easyWorld.Enemies[0].Position;
        hardWorld.Player.Position = hardWorld.Enemies[0].Position;

        Step(easyWorld, Vector2.Zero);
        Step(hardWorld, Vector2.Zero);

        Assert.Equal(PlayerState.MaxHealth - 1, easyWorld.Player.Health);
        Assert.Equal(PlayerState.MaxHealth - 2, hardWorld.Player.Health);
    }

    [Fact]
    public void Boss_AwakensWhenApproached_AndDiesToShots()
    {
        var world = NewWorld(Difficulty.Dusk);
        var boss = world.Boss;
        boss.Health = 2;

        world.Player.Position = boss.Position - new Vector2(150f, 0f);
        Step(world, Vector2.Zero);
        Assert.True(boss.Awake);
        Assert.Contains(world.Events, e => e.Type == WorldEventType.BossAwakened);

        var events = StepEvents(world, Vector2.Zero, attack: true, frames: 20);

        Assert.False(boss.IsAlive);
        Assert.True(world.PortalActive);
        Assert.Contains(WorldEventType.BossDefeated, events);
    }

    [Fact]
    public void Portal_ClearsLevelOnceBossIsGone()
    {
        var world = NewWorld(Difficulty.Dusk);
        world.Boss.Health = 0;

        world.Player.Position = world.Map.PortalPosition;
        Step(world, Vector2.Zero);

        Assert.Equal(WorldStatus.Cleared, world.Status);
        Assert.True(world.Score > 0);
        Assert.Contains(world.Events, e => e.Type == WorldEventType.LevelCleared);
    }

    [Fact]
    public void PlayerDeath_SetsFailedStatus()
    {
        var world = NewWorld(health: 1);
        var enemy = world.Enemies.First();

        world.Player.Position = enemy.Position;
        Step(world, Vector2.Zero);

        Assert.Equal(0, world.Player.Health);
        Assert.Equal(WorldStatus.Failed, world.Status);
        Assert.Contains(world.Events, e => e.Type == WorldEventType.PlayerDied);
    }

    [Fact]
    public void Infinite_Update_AfterFailure_IsIgnored()
    {
        var world = NewWorld(health: 1);
        world.Player.Position = world.Enemies[0].Position;
        Step(world, Vector2.Zero);
        var position = world.Player.Position;

        Step(world, Vector2.UnitX, frames: 60);

        Assert.Equal(WorldStatus.Failed, world.Status);
        Assert.Equal(position, world.Player.Position);
    }

    [Fact]
    public void HardMode_AddsExtraEnemiesAndTougherOnes()
    {
        var easy = NewWorld(Difficulty.Dusk);
        var hard = NewWorld(Difficulty.EternalNight);

        Assert.True(hard.Enemies.Count > easy.Enemies.Count);
        Assert.True(hard.Boss.MaxHealth > easy.Boss.MaxHealth);

        var easyCrawler = easy.Enemies.First(e => e.Kind == EnemyKind.Crawler);
        var hardCrawler = hard.Enemies.First(e => e.Kind == EnemyKind.Crawler);
        Assert.True(hardCrawler.MaxHealth > easyCrawler.MaxHealth);
    }

    [Fact]
    public void Attack_IsRateLimited()
    {
        var world = NewWorld();
        var enemy = world.Enemies.First();

        world.Player.Position = enemy.Position - new Vector2(300f, 0f);
        Step(world, Vector2.Zero, attack: true);
        var firstCount = world.Projectiles.Count;

        Step(world, Vector2.Zero, attack: true);
        Assert.Equal(firstCount, world.Projectiles.Count);

        world.Player.AttackCooldown = 0f;
        Step(world, Vector2.Zero, attack: true);
        Assert.True(world.Projectiles.Count >= firstCount);
    }

    [Fact]
    public void SavingThrow_HealthNeverDropsBelowZero()
    {
        var world = NewWorld(health: 1);
        world.Player.Position = world.Enemies[0].Position;
        Step(world, Vector2.Zero);
        Step(world, Vector2.Zero);

        Assert.Equal(0, world.Player.Health);
    }
}
