using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using Mutants.Core;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Mutants.Game.Rendering;

public sealed class ParticleSystem
{
    private readonly List<Particle> _particles = new();
    private readonly Random _random = new(90210);

    public IReadOnlyList<Particle> Particles => _particles;

    public void Clear() => _particles.Clear();

    public void Burst(Vector2 position, Color color, int count, float speed = 180f, float lifetime = 0.8f, float size = 22f)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(_random.NextDouble() * MathF.Tau);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (speed * (0.35f + (float)_random.NextDouble()));
            _particles.Add(new Particle
            {
                Position = position,
                Velocity = velocity,
                Life = lifetime * (0.6f + (float)_random.NextDouble() * 0.7f),
                MaxLife = lifetime,
                Size = size * (0.6f + (float)_random.NextDouble() * 0.9f),
                Color = color,
                Drag = 2.8f,
                Gravity = 20f,
            });
        }
    }

    public void Trail(Vector2 position, Vector2 direction, Color color, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _particles.Add(new Particle
            {
                Position = position + (direction * (float)_random.NextDouble() * 12f),
                Velocity = -direction * (30f + (float)_random.NextDouble() * 70f) +
                           new Vector2((float)_random.NextDouble() * 30f - 15f, (float)_random.NextDouble() * 30f - 15f),
                Life = 0.35f,
                MaxLife = 0.35f,
                Size = 12f,
                Color = color,
                Drag = 4f,
                Gravity = 0f,
            });
        }
    }

    public void Ambient(LevelTheme theme, RectangleF view, float dt)
    {
        var color = theme switch
        {
            LevelTheme.Graveyard => new Color((byte)182, (byte)242, (byte)107),
            LevelTheme.Mire => new Color((byte)95, (byte)224, (byte)192),
            LevelTheme.Foundry => new Color((byte)255, (byte)158, (byte)61),
            LevelTheme.Chapel => new Color((byte)196, (byte)226, (byte)255),
            _ => new Color((byte)255, (byte)90, (byte)95),
        };

        var spawnChance = theme == LevelTheme.Chapel ? 3 : 1;
        for (var i = 0; i < spawnChance; i++)
        {
            if (_random.NextDouble() > dt * 14)
            {
                continue;
            }

            var position = new Vector2(
                view.X + ((float)_random.NextDouble() * view.Width),
                view.Y + ((float)_random.NextDouble() * view.Height));

            var velocity = theme switch
            {
                LevelTheme.Chapel => new Vector2(-18f + (float)_random.NextDouble() * 36f, 18f + (float)_random.NextDouble() * 26f),
                LevelTheme.Foundry => new Vector2(-10f + (float)_random.NextDouble() * 20f, -30f - (float)_random.NextDouble() * 30f),
                LevelTheme.Mire => new Vector2(-8f + (float)_random.NextDouble() * 16f, -14f - (float)_random.NextDouble() * 20f),
                _ => new Vector2(-12f + (float)_random.NextDouble() * 24f, -10f - (float)_random.NextDouble() * 24f),
            };

            _particles.Add(new Particle
            {
                Position = position,
                Velocity = velocity,
                Life = 2.4f + (float)_random.NextDouble() * 2.4f,
                MaxLife = 4.8f,
                Size = 10f + (float)_random.NextDouble() * 12f,
                Color = color,
                Drag = 0.1f,
                Gravity = -4f,
                Flicker = true,
            });
        }

        if (_particles.Count > 160)
        {
            _particles.RemoveRange(0, _particles.Count - 160);
        }
    }

    public void Update(float dt)
    {
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Life -= dt;
            if (particle.Life <= 0f)
            {
                _particles.RemoveAt(i);
                continue;
            }

            var velocity = particle.Velocity * MathF.Max(0f, 1f - (particle.Drag * dt));
            velocity.Y += particle.Gravity * dt;
            particle.Velocity = velocity;
            particle.Position += velocity * dt;
        }
    }

    public void Draw(SpriteBatch batch, Texture2D texture, Microsoft.Xna.Framework.Vector2 offset, float time)
    {
        foreach (var particle in _particles)
        {
            var alpha = Math.Clamp(particle.Life / particle.MaxLife, 0f, 1f);
            if (particle.Flicker)
            {
                alpha *= 0.55f + (MathF.Sin((time * 7f) + (particle.Position.X * 0.05f)) * 0.45f);
            }

            var tint = new Color(particle.Color.R, particle.Color.G, particle.Color.B, (byte)Math.Clamp(alpha * 190f, 0f, 255f));
            var destination = new Rectangle(
                (int)(particle.Position.X + offset.X - (particle.Size / 2f)),
                (int)(particle.Position.Y + offset.Y - (particle.Size / 2f)),
                (int)particle.Size,
                (int)particle.Size);
            batch.Draw(texture, destination, tint);
        }
    }
}

public sealed class Particle
{
    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public float Life { get; set; }

    public float MaxLife { get; set; }

    public float Size { get; set; }

    public Color Color { get; set; }

    public float Drag { get; set; }

    public float Gravity { get; set; }

    public bool Flicker { get; set; }
}

public readonly record struct RectangleF(float X, float Y, float Width, float Height);
