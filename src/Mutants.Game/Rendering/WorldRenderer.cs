using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mutants.Core;
using NVector2 = System.Numerics.Vector2;

namespace Mutants.Game.Rendering;

/// <summary>Draws a <see cref="GameWorld"/> in the 1280x720 virtual space.</summary>
public sealed class WorldRenderer
{
    private readonly ArtFactory _art;

    public WorldRenderer(ArtFactory art)
    {
        _art = art;
    }

    public void Draw(SpriteBatch batch, GameWorld world, Vector2 camera, float time)
    {
        var theme = _art.Themes[world.Map.Theme];
        var map = world.Map;

        batch.Draw(theme.Sky, new Rectangle(0, 0, 1280, 720), Color.White);

        var offset = -camera;
        var firstColumn = Math.Max(0, (int)(camera.X / LevelMap.TileSize) - 1);
        var lastColumn = Math.Min(map.Columns - 1, (int)((camera.X + 1280) / LevelMap.TileSize) + 1);
        var firstRow = Math.Max(0, (int)(camera.Y / LevelMap.TileSize) - 1);
        var lastRow = Math.Min(map.Rows - 1, (int)((camera.Y + 720) / LevelMap.TileSize) + 1);

        // Ground layer.
        for (var row = firstRow; row <= lastRow; row++)
        {
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var x = (column * LevelMap.TileSize) + (int)offset.X;
                var y = (row * LevelMap.TileSize) + (int)offset.Y;
                var tile = map[column, row];
                var destination = new Rectangle(x, y, LevelMap.TileSize, LevelMap.TileSize);

                if (tile == TileKind.Water)
                {
                    var water = ((int)(time * 1.4f) + column + row) % 2 == 0 ? theme.WaterA : theme.WaterB;
                    batch.Draw(water, destination, Color.White);
                    continue;
                }

                var variant = Math.Abs((column * 7) + (row * 13) + (int)world.Map.Theme * 3) % theme.Floors.Length;
                batch.Draw(theme.Floors[variant], destination, Color.White);

                if (tile == TileKind.Wall)
                {
                    batch.Draw(
                        theme.WallA,
                        new Rectangle(x + 3, y + 8, LevelMap.TileSize, LevelMap.TileSize),
                        new Color((byte)0, (byte)0, (byte)0, (byte)70));
                    var wall = ((column + row) % 5 == 0) ? theme.WallB : theme.WallA;
                    batch.Draw(wall, destination, Color.White);
                }
            }
        }

        // Gate sits on top of the floor.
        var gate = map.GatePosition;
        if (world.GateOpen)
        {
            batch.Draw(
                _art.Gate,
                new Rectangle((int)(gate.X + offset.X) - 60, (int)(gate.Y + offset.Y) - 142, 120, 190),
                new Color((byte)255, (byte)255, (byte)255, (byte)54));
        }
        else
        {
            batch.Draw(
                _art.Gate,
                new Rectangle((int)(gate.X + offset.X) - 60, (int)(gate.Y + offset.Y) - 142, 120, 190),
                Color.White);
        }

        // Portal.
        var portal = map.PortalPosition;
        var portalPulse = 1f + (MathF.Sin(time * 2.2f) * 0.05f);
        var portalAlpha = world.PortalActive ? 1f : 0.18f;
        var portalSize = new Rectangle(
            (int)(portal.X + offset.X) - 110,
            (int)(portal.Y + offset.Y) - 180,
            (int)(220 * portalPulse),
            (int)(250 * portalPulse));
        batch.Draw(_art.Portal, portalSize, new Color((byte)255, (byte)255, (byte)255, (byte)(255 * portalAlpha)));
        if (world.PortalActive)
        {
            batch.Draw(
                _art.SoftGlow,
                new Rectangle((int)(portal.X + offset.X) - 160, (int)(portal.Y + offset.Y) - 210, 320, 320),
                new Color((byte)122, (byte)92, (byte)255, (byte)70));
        }

        // Scenery.
        for (var row = firstRow; row <= lastRow; row++)
        {
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var tile = map[column, row];
                var centerX = (column * LevelMap.TileSize) + 48 + (int)offset.X;
                var bottomY = (row * LevelMap.TileSize) + 96 + (int)offset.Y;

                if (tile == TileKind.Tree)
                {
                    var height = (int)(220 * 0.92f);
                    var width = (int)(150 * 0.92f);
                    batch.Draw(_art.Shadow, new Rectangle(centerX - 50, bottomY - 18, 100, 46), Color.White);
                    batch.Draw(theme.Tree, new Rectangle(centerX - (width / 2), bottomY - height, width, height), Color.White);
                }
                else if (tile == TileKind.Rock)
                {
                    var width = (int)(120 * 0.9f);
                    var height = (int)(100 * 0.9f);
                    batch.Draw(_art.Shadow, new Rectangle(centerX - 44, bottomY - 18, 88, 38), Color.White);
                    batch.Draw(theme.Rock, new Rectangle(centerX - (width / 2), bottomY - height, width, height), Color.White);
                }
            }
        }

        // Pickups.
        foreach (var pickup in world.Pickups)
        {
            if (pickup.Collected)
            {
                continue;
            }

            var bob = MathF.Sin((time * 2.4f) + pickup.Phase) * 6f;
            var position = new Vector2(pickup.Position.X + offset.X, pickup.Position.Y + offset.Y + bob);
            var texture = pickup.Kind switch
            {
                PickupKind.Key => _art.Key,
                PickupKind.Coin => _art.Coin,
                _ => _art.Heart,
            };
            var size = pickup.Kind switch
            {
                PickupKind.Key => 52,
                PickupKind.Coin => 34,
                _ => 42,
            };

            var glowColor = pickup.Kind switch
            {
                PickupKind.Key => new Color((byte)245, (byte)196, (byte)81, (byte)90),
                PickupKind.Coin => new Color((byte)196, (byte)214, (byte)226, (byte)70),
                _ => new Color((byte)210, (byte)65, (byte)80, (byte)90),
            };

            batch.Draw(
                _art.SoftGlow,
                new Rectangle((int)position.X - (size * 2), (int)position.Y - (size * 2), size * 4, size * 4),
                glowColor);
            batch.Draw(texture, new Rectangle((int)position.X - (size / 2), (int)position.Y - (size / 2), size, size), Color.White);
        }

        // Enemies, boss and player sorted by vertical position.
        var drawables = new List<(float Y, Action Draw)>(world.Enemies.Count + 2);

        foreach (var enemy in world.Enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            var enemyCopy = enemy;
            drawables.Add((enemy.Position.Y, () => DrawEnemy(batch, enemyCopy, offset, time)));
        }

        if (world.Boss.IsAlive)
        {
            drawables.Add((world.Boss.Position.Y, () => DrawBoss(batch, world.Boss, offset, time)));
        }

        drawables.Add((world.Player.Position.Y, () => DrawPlayer(batch, world, offset, time)));

        drawables.Sort((a, b) => a.Y.CompareTo(b.Y));
        foreach (var drawable in drawables)
        {
            drawable.Draw();
        }

        // Projectiles.
        foreach (var projectile in world.Projectiles)
        {
            var position = new Vector2(projectile.Position.X + offset.X, projectile.Position.Y + offset.Y);
            var texture = projectile.FromPlayer ? _art.PlayerBolt : _art.BossBolt;
            var rotation = MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X);

            if (projectile.FromPlayer)
            {
                batch.Draw(
                    texture,
                    position,
                    null,
                    Color.White,
                    rotation,
                    new Vector2(32, 14),
                    0.9f,
                    SpriteEffects.None,
                    0f);
            }
            else
            {
                batch.Draw(
                    texture,
                    position,
                    null,
                    Color.White,
                    rotation,
                    new Vector2(24, 24),
                    1f,
                    SpriteEffects.None,
                    0f);
            }
        }

        // Lantern and gate/key light.
        var lantern = new Vector2(world.Player.Position.X + offset.X + (world.Player.Facing.X * 16f), world.Player.Position.Y + offset.Y - 8f);
        batch.Draw(
            _art.Glow,
            new Rectangle((int)lantern.X - 170, (int)lantern.Y - 170, 340, 340),
            new Color((byte)255, (byte)174, (byte)84, (byte)55));

        if (!world.GateOpen)
        {
            batch.Draw(
                _art.SoftGlow,
                new Rectangle((int)(gate.X + offset.X) - 90, (int)(gate.Y + offset.Y) - 120, 180, 180),
                new Color((byte)255, (byte)158, (byte)61, (byte)35));
        }

        // Fog wash.
        var fogX = (time * 18f) % 512;
        batch.Draw(
            _art.Fog,
            new Rectangle((int)(-fogX), 0, 1280 + 512, 720),
            new Color(theme.Fog.R, theme.Fog.G, theme.Fog.B, (byte)45));
    }

    private void DrawPlayer(SpriteBatch batch, GameWorld world, Vector2 offset, float time)
    {
        var player = world.Player;
        var position = new Vector2(player.Position.X + offset.X, player.Position.Y + offset.Y);
        var moving = world.Events.Any(e => e.Type == WorldEventType.ShotFired) || player.AttackCooldown > 0.05f;
        var frame = (int)(time * 7f) % 2 == 0 ? _art.PlayerA : _art.PlayerB;
        var flip = player.Facing.X < -0.15f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        batch.Draw(_art.Shadow, new Rectangle((int)position.X - 42, (int)position.Y + 2, 84, 34), Color.White);

        var tint = player.InvulnerableTime > 0f && ((int)(time * 18f) % 2 == 0)
            ? new Color((byte)255, (byte)255, (byte)255, (byte)150)
            : Color.White;

        batch.Draw(
            frame,
            position + new Vector2(0, 18),
            null,
            tint,
            0f,
            new Vector2(60, 148),
            0.86f,
            flip,
            0f);
    }

    private void DrawEnemy(SpriteBatch batch, EnemyState enemy, Vector2 offset, float time)
    {
        var position = new Vector2(enemy.Position.X + offset.X, enemy.Position.Y + offset.Y);
        var frameToggle = (int)((time * (enemy.Kind == EnemyKind.Bat ? 11f : 5.5f)) + enemy.Phase) % 2 == 0;

        var texture = enemy.Kind switch
        {
            EnemyKind.Crawler => frameToggle ? _art.CrawlerA : _art.CrawlerB,
            EnemyKind.Bat => frameToggle ? _art.BatA : _art.BatB,
            _ => frameToggle ? _art.GhoulA : _art.GhoulB,
        };

        var scale = enemy.Kind switch
        {
            EnemyKind.Crawler => 0.95f,
            EnemyKind.Bat => 1f,
            _ => 0.95f,
        };

        var origin = enemy.Kind switch
        {
            EnemyKind.Crawler => new Vector2(55, 82),
            EnemyKind.Bat => new Vector2(65, 64),
            _ => new Vector2(60, 154),
        };

        var flip = enemy.Position.X > 0 && ((int)(enemy.Position.X + time * 3f) % 2 == 0)
            ? SpriteEffects.None
            : SpriteEffects.FlipHorizontally;

        batch.Draw(_art.Shadow, new Rectangle((int)position.X - 34, (int)position.Y + (enemy.Kind == EnemyKind.Bat ? -18 : 4), 68, 26), Color.White);

        var tint = enemy.HitFlash > 0f ? new Color((byte)255, (byte)255, (byte)255) : Color.White;
        if (enemy.Kind == EnemyKind.Ghoul)
        {
            tint = tint * 0.95f;
        }

        batch.Draw(texture, position + new Vector2(0, enemy.Kind == EnemyKind.Bat ? 0 : 18), null, tint, 0f, origin, scale, flip, 0f);

        if (enemy.Health < enemy.MaxHealth)
        {
            var width = (int)(enemy.Radius * 1.5f);
            var healthRect = new Rectangle((int)position.X - (width / 2), (int)position.Y - (int)(enemy.Radius * 1.55f), width, 4);
            batch.Draw(_art.Panel, healthRect, new Color((byte)9, (byte)6, (byte)14, (byte)170));
            batch.Draw(
                _art.Panel,
                new Rectangle(healthRect.X, healthRect.Y, (int)(width * ((float)enemy.Health / enemy.MaxHealth)), healthRect.Height),
                enemy.Kind == EnemyKind.Bat ? new Color((byte)196, (byte)120, (byte)255) : new Color((byte)182, (byte)242, (byte)107));
        }
    }

    private void DrawBoss(SpriteBatch batch, BossState boss, Vector2 offset, float time)
    {
        var position = new Vector2(boss.Position.X + offset.X, boss.Position.Y + offset.Y);
        var bob = MathF.Sin(time * 1.8f) * 4f;
        var alpha = boss.Awake ? 1f : 0.82f;
        var tint = boss.HitFlash > 0f
            ? new Color((byte)255, (byte)225, (byte)225)
            : new Color((byte)255, (byte)255, (byte)255, (byte)(255 * alpha));

        batch.Draw(_art.Shadow, new Rectangle((int)position.X - 100, (int)position.Y + 16, 200, 54), Color.White);
        batch.Draw(
            _art.SoftGlow,
            new Rectangle((int)position.X - 150, (int)position.Y - 180, 300, 300),
            boss.Awake
                ? new Color((byte)255, (byte)70, (byte)80, (byte)65)
                : new Color((byte)122, (byte)92, (byte)255, (byte)45));

        batch.Draw(
            _art.Boss,
            position + new Vector2(0, 24 + bob),
            null,
            tint,
            0f,
            new Vector2(120, 252),
            0.92f,
            SpriteEffects.None,
            0f);
    }
}
