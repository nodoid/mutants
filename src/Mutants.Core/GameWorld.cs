using System.Numerics;

namespace Mutants.Core;

public readonly record struct GameInput(Vector2 Move, bool Attack)
{
    public static GameInput None => new(Vector2.Zero, false);
}

public enum WorldStatus
{
    Playing,
    Cleared,
    Failed,
}

public enum WorldEventType
{
    Coin,
    Key,
    Heart,
    EnemyHit,
    EnemyKilled,
    PlayerHurt,
    GateOpened,
    BossAwakened,
    BossDefeated,
    LevelCleared,
    PlayerDied,
    ShotFired,
}

public readonly record struct WorldEvent(WorldEventType Type, Vector2 Position, int Value = 0);

public sealed class PlayerState
{
    public const int MaxHealth = 6;

    internal PlayerState(Vector2 position, int health)
    {
        Position = position;
        Health = Math.Clamp(health, 1, MaxHealth);
        Facing = Vector2.UnitX;
    }

    public Vector2 Position { get; internal set; }

    public Vector2 Facing { get; internal set; }

    public int Health { get; internal set; }

    public float Radius { get; internal set; } = 22f;

    public float Speed { get; internal set; } = 300f;

    public float InvulnerableTime { get; internal set; }

    public float AttackCooldown { get; internal set; }

    public bool IsAlive => Health > 0;
}

public sealed class EnemyState
{
    internal EnemyState(EnemyKind kind, Vector2 position, Vector2 home, float speed, int health)
    {
        Kind = kind;
        Position = position;
        Home = home;
        Speed = speed;
        Health = health;
        MaxHealth = health;
        Phase = (position.X + position.Y) * 0.013f;
        Radius = kind switch
        {
            EnemyKind.Crawler => 24f,
            EnemyKind.Bat => 21f,
            EnemyKind.Ghoul => 30f,
            _ => 24f,
        };
    }

    public EnemyKind Kind { get; }

    public Vector2 Position { get; internal set; }

    public Vector2 Home { get; }

    public float Radius { get; }

    public float Speed { get; }

    public int Health { get; internal set; }

    public int MaxHealth { get; }

    public float Phase { get; }

    public float HitFlash { get; internal set; }

    public bool IsAlive => Health > 0;

    public int ScoreValue => Kind switch
    {
        EnemyKind.Crawler => 100,
        EnemyKind.Bat => 130,
        EnemyKind.Ghoul => 180,
        _ => 100,
    };
}

public sealed class BossState
{
    internal BossState(Vector2 position, int health)
    {
        Position = position;
        Health = health;
        MaxHealth = health;
        FireTimer = 1.4f;
    }

    public Vector2 Position { get; internal set; }

    public float Radius { get; } = 54f;

    public int Health { get; internal set; }

    public int MaxHealth { get; }

    public bool Awake { get; internal set; }

    public bool IsAlive => Health > 0;

    public float HitFlash { get; internal set; }

    public float FireTimer { get; internal set; }

    public float Phase { get; internal set; }
}

public sealed class PickupState
{
    internal PickupState(PickupKind kind, Vector2 position)
    {
        Kind = kind;
        Position = position;
        Phase = (position.X * 0.02f) + (position.Y * 0.01f);
    }

    public PickupKind Kind { get; }

    public Vector2 Position { get; }

    public float Phase { get; }

    public bool Collected { get; internal set; }
}

public sealed class ProjectileState
{
    internal ProjectileState(Vector2 position, Vector2 velocity, float lifetime, int damage, bool fromPlayer)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        Damage = damage;
        FromPlayer = fromPlayer;
        Radius = fromPlayer ? 9f : 12f;
    }

    public Vector2 Position { get; internal set; }

    public Vector2 Velocity { get; }

    public float Radius { get; }

    public float Lifetime { get; internal set; }

    public int Damage { get; }

    public bool FromPlayer { get; }
}

public static class ScoreValues
{
    public const int Coin = 25;
    public const int Key = 150;
    public const int Boss = 1500;
    public const int LevelClear = 750;
    public const int HealthBonus = 50;
    public const int Heart = 25;
}

/// <summary>
/// One level's living world. All rules run at a fixed logical scale so that
/// simulation can be tested without a graphics device.
/// </summary>
public sealed class GameWorld
{
    private const float EnemyAggroRange = 660f;
    private const float PlayerAttackCooldown = 0.26f;
    private const float PlayerProjectileSpeed = 820f;
    private const float InvulnerabilitySeconds = 1.15f;

    private readonly List<EnemyState> _enemies = new();
    private readonly List<PickupState> _pickups = new();
    private readonly List<ProjectileState> _projectiles = new();
    private readonly List<WorldEvent> _events = new();
    private bool _gateOpen;
    private int _keysCollected;

    public GameWorld(LevelMap map, DifficultyProfile difficulty, int startingHealth = PlayerState.MaxHealth)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        Player = new PlayerState(map.SpawnPosition, startingHealth);
        Boss = new BossState(map.BossPosition, difficulty.BossHealth);
        Score = 0;

        foreach (var spawn in map.Enemies)
        {
            _enemies.Add(CreateEnemy(spawn.Kind, LevelMap.CellCenter(spawn.Column, spawn.Row)));
        }

        AddDifficultyExtras();

        foreach (var spawn in map.Pickups)
        {
            _pickups.Add(new PickupState(spawn.Kind, LevelMap.CellCenter(spawn.Column, spawn.Row)));
        }
    }

    public LevelMap Map { get; }

    public DifficultyProfile Difficulty { get; }

    public PlayerState Player { get; }

    public BossState Boss { get; }

    public IReadOnlyList<EnemyState> Enemies => _enemies;

    public IReadOnlyList<PickupState> Pickups => _pickups;

    public IReadOnlyList<ProjectileState> Projectiles => _projectiles;

    public IReadOnlyList<WorldEvent> Events => _events;

    public int Score { get; private set; }

    public int KeysCollected => _keysCollected;

    public int RequiredKeys => Map.RequiredKeys;

    public bool GateOpen => _gateOpen;

    public bool PortalActive => !Boss.IsAlive;

    public WorldStatus Status { get; private set; } = WorldStatus.Playing;

    public float Time { get; private set; }

    public void Update(float dt, GameInput input)
    {
        if (Status != WorldStatus.Playing || dt <= 0f)
        {
            return;
        }

        dt = MathF.Min(dt, 0.05f);
        Time += dt;
        _events.Clear();

        UpdatePlayer(dt, input);
        UpdatePickups();
        UpdateEnemies(dt);
        UpdateBoss(dt);
        UpdateProjectiles(dt);

        if (Player.Health <= 0)
        {
            Player.Health = 0;
            Status = WorldStatus.Failed;
            Emit(WorldEventType.PlayerDied, Player.Position);
            return;
        }

        if (PortalActive)
        {
            var dx = Player.Position.X - Map.PortalPosition.X;
            var dy = Player.Position.Y - Map.PortalPosition.Y;
            if ((dx * dx) + (dy * dy) < 86f * 86f)
            {
                Score += Weighted(ScoreValues.LevelClear) + (Player.Health * ScoreValues.HealthBonus);
                Status = WorldStatus.Cleared;
                Emit(WorldEventType.LevelCleared, Map.PortalPosition, Score);
            }
        }
    }

    private void UpdatePlayer(float dt, GameInput input)
    {
        Player.InvulnerableTime = MathF.Max(0f, Player.InvulnerableTime - dt);
        Player.AttackCooldown = MathF.Max(0f, Player.AttackCooldown - dt);

        var move = input.Move;
        if (move.LengthSquared() > 1f)
        {
            move = Vector2.Normalize(move);
        }

        if (move.LengthSquared() > 0.01f)
        {
            Player.Facing = Vector2.Normalize(move);
        }

        var speed = Player.Speed;
        TryMove(move * speed * dt);

        if (input.Attack && Player.AttackCooldown <= 0f)
        {
            FirePlayerProjectile();
        }
    }

    private void TryMove(Vector2 delta)
    {
        var position = Player.Position;

        var candidate = new Vector2(position.X + delta.X, position.Y);
        if (!Map.IsBlocked(candidate, Player.Radius, _gateOpen))
        {
            position = candidate;
        }

        candidate = new Vector2(position.X, position.Y + delta.Y);
        if (!Map.IsBlocked(candidate, Player.Radius, _gateOpen))
        {
            position = candidate;
        }

        Player.Position = new Vector2(
            Math.Clamp(position.X, Player.Radius, Map.Width - Player.Radius),
            Math.Clamp(position.Y, Player.Radius, Map.Height - Player.Radius));
    }

    private void FirePlayerProjectile()
    {
        var direction = AimDirection();
        var origin = Player.Position + (direction * (Player.Radius + 12f));

        _projectiles.Add(new ProjectileState(
            origin,
            direction * PlayerProjectileSpeed,
            0.62f,
            damage: 2,
            fromPlayer: true));

        Player.AttackCooldown = PlayerAttackCooldown;
        Emit(WorldEventType.ShotFired, origin);
    }

    private Vector2 AimDirection()
    {
        var bestDistance = 780f;
        Vector2? best = null;

        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            var distance = Vector2.Distance(enemy.Position, Player.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy.Position;
            }
        }

        if (Boss.IsAlive && Boss.Awake)
        {
            var distance = Vector2.Distance(Boss.Position, Player.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = Boss.Position;
            }
        }

        if (best is { } target)
        {
            var direction = target - Player.Position;
            if (direction.LengthSquared() > 0.01f)
            {
                return Vector2.Normalize(direction);
            }
        }

        return Player.Facing;
    }

    private void UpdatePickups()
    {
        foreach (var pickup in _pickups)
        {
            if (pickup.Collected)
            {
                continue;
            }

            if (Vector2.DistanceSquared(pickup.Position, Player.Position) > 46f * 46f)
            {
                continue;
            }

            switch (pickup.Kind)
            {
                case PickupKind.Key:
                    pickup.Collected = true;
                    _keysCollected++;
                    AddScore(ScoreValues.Key);
                    Emit(WorldEventType.Key, pickup.Position, _keysCollected);
                    if (_keysCollected >= RequiredKeys && !_gateOpen)
                    {
                        _gateOpen = true;
                        Emit(WorldEventType.GateOpened, Map.GatePosition);
                    }

                    break;

                case PickupKind.Coin:
                    pickup.Collected = true;
                    AddScore(ScoreValues.Coin);
                    Emit(WorldEventType.Coin, pickup.Position);
                    break;

                case PickupKind.Heart:
                    if (Player.Health >= PlayerState.MaxHealth)
                    {
                        continue;
                    }

                    pickup.Collected = true;
                    Player.Health = Math.Min(PlayerState.MaxHealth, Player.Health + 2);
                    AddScore(ScoreValues.Heart);
                    Emit(WorldEventType.Heart, pickup.Position);
                    break;
            }
        }
    }

    private void UpdateEnemies(float dt)
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            enemy.HitFlash = MathF.Max(0f, enemy.HitFlash - dt);

            var toPlayer = Player.Position - enemy.Position;
            var distance = toPlayer.Length();
            Vector2 direction;

            if (distance < EnemyAggroRange)
            {
                direction = distance > 0.01f ? toPlayer / distance : Vector2.UnitX;

                if (enemy.Kind == EnemyKind.Bat)
                {
                    var wobble = MathF.Sin((Time * 5.2f) + enemy.Phase) * 0.55f;
                    direction = Vector2.Normalize(Vector2.Transform(direction, Matrix3x2.CreateRotation(wobble)));
                }
            }
            else
            {
                var orbit = new Vector2(
                    MathF.Cos((Time * 0.7f) + enemy.Phase) * 92f,
                    MathF.Sin((Time * 0.55f) + enemy.Phase) * 72f);
                var target = enemy.Home + orbit;
                var toHome = target - enemy.Position;
                direction = toHome.LengthSquared() > 4f ? Vector2.Normalize(toHome) : Vector2.Zero;
            }

            if (direction != Vector2.Zero)
            {
                MoveEnemy(enemy, direction * enemy.Speed * dt);
            }

            var touchDistance = enemy.Radius + Player.Radius - 4f;
            if (Vector2.DistanceSquared(enemy.Position, Player.Position) < touchDistance * touchDistance)
            {
                HurtPlayer(Difficulty.ContactDamage, enemy.Position);
            }
        }
    }

    private void MoveEnemy(EnemyState enemy, Vector2 delta)
    {
        var position = enemy.Position;
        var candidate = new Vector2(position.X + delta.X, position.Y);
        if (!Map.IsBlocked(candidate, enemy.Radius, _gateOpen))
        {
            position = candidate;
        }

        candidate = new Vector2(position.X, position.Y + delta.Y);
        if (!Map.IsBlocked(candidate, enemy.Radius, _gateOpen))
        {
            position = candidate;
        }

        enemy.Position = new Vector2(
            Math.Clamp(position.X, enemy.Radius, Map.Width - enemy.Radius),
            Math.Clamp(position.Y, enemy.Radius, Map.Height - enemy.Radius));
    }

    private void UpdateBoss(float dt)
    {
        if (!Boss.IsAlive)
        {
            return;
        }

        Boss.HitFlash = MathF.Max(0f, Boss.HitFlash - dt);
        Boss.Phase += dt;

        var toPlayer = Player.Position - Boss.Position;
        var distance = toPlayer.Length();

        if (!Boss.Awake && distance < 620f)
        {
            Boss.Awake = true;
            Emit(WorldEventType.BossAwakened, Boss.Position);
        }

        if (!Boss.Awake)
        {
            return;
        }

        if (distance > 170f && distance > 0.01f)
        {
            var direction = toPlayer / distance;
            var delta = direction * 78f * Difficulty.EnemySpeedMultiplier * dt;
            var position = Boss.Position;

            var candidate = new Vector2(position.X + delta.X, position.Y);
            if (!Map.IsBlocked(candidate, Boss.Radius, _gateOpen))
            {
                position = candidate;
            }

            candidate = new Vector2(position.X, position.Y + delta.Y);
            if (!Map.IsBlocked(candidate, Boss.Radius, _gateOpen))
            {
                position = candidate;
            }

            Boss.Position = position;
        }

        var touchDistance = Boss.Radius + Player.Radius - 8f;
        if (Vector2.DistanceSquared(Boss.Position, Player.Position) < touchDistance * touchDistance)
        {
            HurtPlayer(Difficulty.ContactDamage, Boss.Position);
        }

        Boss.FireTimer -= dt;
        if (Boss.FireTimer <= 0f)
        {
            FireBossVolley();
            Boss.FireTimer = 1.65f;
        }
    }

    private void FireBossVolley()
    {
        var baseDirection = Player.Position - Boss.Position;
        if (baseDirection.LengthSquared() < 1f)
        {
            baseDirection = Vector2.UnitX;
        }

        baseDirection = Vector2.Normalize(baseDirection);
        var origin = Boss.Position + (baseDirection * 24f);

        for (var i = -1; i <= 1; i++)
        {
            var direction = Vector2.Transform(baseDirection, Matrix3x2.CreateRotation(i * 0.2f));
            _projectiles.Add(new ProjectileState(
                origin,
                direction * 300f,
                3.2f,
                Math.Max(1, Difficulty.ContactDamage),
                fromPlayer: false));
        }
    }

    private void UpdateProjectiles(float dt)
    {
        for (var i = _projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _projectiles[i];
            projectile.Lifetime -= dt;
            projectile.Position += projectile.Velocity * dt;

            var remove = projectile.Lifetime <= 0f;

            if (!remove && Map.IsBlocked(projectile.Position, projectile.Radius, _gateOpen))
            {
                remove = true;
            }

            if (!remove && projectile.FromPlayer)
            {
                remove = HitEnemies(projectile);
            }
            else if (!remove)
            {
                var touch = projectile.Radius + Player.Radius;
                if (Vector2.DistanceSquared(projectile.Position, Player.Position) < touch * touch)
                {
                    HurtPlayer(projectile.Damage, projectile.Position);
                    remove = true;
                }
            }

            if (remove)
            {
                _projectiles.RemoveAt(i);
            }
        }
    }

    private bool HitEnemies(ProjectileState projectile)
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            var touch = projectile.Radius + enemy.Radius;
            if (Vector2.DistanceSquared(projectile.Position, enemy.Position) >= touch * touch)
            {
                continue;
            }

            enemy.Health -= projectile.Damage;
            enemy.HitFlash = 0.16f;
            Emit(WorldEventType.EnemyHit, projectile.Position);

            if (!enemy.IsAlive)
            {
                AddScore(enemy.ScoreValue);
                Emit(WorldEventType.EnemyKilled, enemy.Position, enemy.ScoreValue);
            }

            return true;
        }

        if (Boss.IsAlive)
        {
            var touch = projectile.Radius + Boss.Radius;
            if (Vector2.DistanceSquared(projectile.Position, Boss.Position) < touch * touch)
            {
                Boss.Health -= projectile.Damage;
                Boss.HitFlash = 0.16f;
                Emit(WorldEventType.EnemyHit, projectile.Position);

                if (!Boss.IsAlive)
                {
                    AddScore(ScoreValues.Boss);
                    Emit(WorldEventType.BossDefeated, Boss.Position, ScoreValues.Boss);
                }

                return true;
            }
        }

        return false;
    }

    private void HurtPlayer(int damage, Vector2 source)
    {
        if (Player.InvulnerableTime > 0f || !Player.IsAlive)
        {
            return;
        }

        Player.Health = Math.Max(0, Player.Health - Math.Max(1, damage));
        Player.InvulnerableTime = InvulnerabilitySeconds;

        var push = Player.Position - source;
        if (push.LengthSquared() > 0.01f)
        {
            push = Vector2.Normalize(push);
            TryMove(push * 26f);
        }

        Emit(WorldEventType.PlayerHurt, Player.Position);
    }

    private void AddDifficultyExtras()
    {
        if (Difficulty.ExtraEnemies <= 0 || _enemies.Count == 0)
        {
            return;
        }

        var offsets = new[]
        {
            new Vector2(58f, -42f),
            new Vector2(-52f, 46f),
            new Vector2(64f, 52f),
        };

        for (var i = 0; i < Difficulty.ExtraEnemies; i++)
        {
            var template = _enemies[i % _enemies.Count];
            var offset = offsets[i % offsets.Length];
            var position = template.Home + offset;

            if (Map.IsBlocked(position, 26f, gateOpen: false))
            {
                continue;
            }

            _enemies.Add(CreateEnemy(template.Kind, position));
        }
    }

    private EnemyState CreateEnemy(EnemyKind kind, Vector2 position)
        => new(kind, position, position, EnemySpeed(kind), EnemyHealth(kind));

    private float EnemySpeed(EnemyKind kind)
    {
        var baseSpeed = kind switch
        {
            EnemyKind.Crawler => 112f,
            EnemyKind.Bat => 176f,
            EnemyKind.Ghoul => 88f,
            _ => 112f,
        };

        return baseSpeed * Difficulty.EnemySpeedMultiplier;
    }

    private int EnemyHealth(EnemyKind kind)
    {
        var baseHealth = kind switch
        {
            EnemyKind.Crawler => 4,
            EnemyKind.Bat => 3,
            EnemyKind.Ghoul => 7,
            _ => 4,
        };

        return Math.Max(1, (int)MathF.Ceiling(baseHealth * Difficulty.EnemyHealthMultiplier));
    }

    private int Weighted(int value) => Math.Max(0, (int)MathF.Round(value * Difficulty.ScoreMultiplier));

    private void AddScore(int value) => Score += Weighted(value);

    private void Emit(WorldEventType type, Vector2 position, int value = 0)
        => _events.Add(new WorldEvent(type, position, value));
}
