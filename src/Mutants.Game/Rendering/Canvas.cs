using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mutants.Game.Rendering;

/// <summary>
/// Tiny software rasteriser. Shapes are drawn at 3x and box-filtered down, which
/// gives smooth, high-resolution edges without any bitmap assets.
/// </summary>
public sealed class Canvas
{
    private readonly Color[] _pixels;
    private readonly int _scale;

    public Canvas(int width, int height, int supersample = 3)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Width = width;
        Height = height;
        _scale = Math.Clamp(supersample, 1, 4);
        _pixels = new Color[width * _scale * height * _scale];
    }

    public int Width { get; }

    public int Height { get; }

    public int ScaledWidth => Width * _scale;

    public int ScaledHeight => Height * _scale;

    public void Clear(Color color)
    {
        var premultiplied = Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);
        Array.Fill(_pixels, premultiplied);
    }

    public void Fill(Color color) => Clear(color);

    public void FillCircle(float centerX, float centerY, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        var cx = centerX * _scale;
        var cy = centerY * _scale;
        var r = radius * _scale;
        var minX = Math.Max(0, (int)MathF.Floor(cx - r - 1f));
        var maxX = Math.Min(ScaledWidth - 1, (int)MathF.Ceiling(cx + r + 1f));
        var minY = Math.Max(0, (int)MathF.Floor(cy - r - 1f));
        var maxY = Math.Min(ScaledHeight - 1, (int)MathF.Ceiling(cy + r + 1f));

        for (var y = minY; y <= maxY; y++)
        {
            var dy = y + 0.5f - cy;
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x + 0.5f - cx;
                var distance = MathF.Sqrt((dx * dx) + (dy * dy));
                var coverage = Math.Clamp(r - distance + 0.5f, 0f, 1f);
                if (coverage > 0f)
                {
                    Plot(x, y, color, coverage);
                }
            }
        }
    }

    public void FillEllipse(float centerX, float centerY, float radiusX, float radiusY, Color color)
    {
        if (radiusX <= 0f || radiusY <= 0f)
        {
            return;
        }

        var cx = centerX * _scale;
        var cy = centerY * _scale;
        var rx = radiusX * _scale;
        var ry = radiusY * _scale;
        var minX = Math.Max(0, (int)MathF.Floor(cx - rx - 1f));
        var maxX = Math.Min(ScaledWidth - 1, (int)MathF.Ceiling(cx + rx + 1f));
        var minY = Math.Max(0, (int)MathF.Floor(cy - ry - 1f));
        var maxY = Math.Min(ScaledHeight - 1, (int)MathF.Ceiling(cy + ry + 1f));
        var edge = MathF.Min(rx, ry);

        for (var y = minY; y <= maxY; y++)
        {
            var dy = (y + 0.5f - cy) / ry;
            for (var x = minX; x <= maxX; x++)
            {
                var dx = (x + 0.5f - cx) / rx;
                var distance = MathF.Sqrt((dx * dx) + (dy * dy));
                var coverage = Math.Clamp(((1f - distance) * edge) + 0.5f, 0f, 1f);
                if (coverage > 0f)
                {
                    Plot(x, y, color, coverage);
                }
            }
        }
    }

    public void FillRect(float x, float y, float width, float height, Color color)
        => FillRoundedRect(x, y, width, height, 0f, color);

    public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        if (width <= 0f || height <= 0f)
        {
            return;
        }

        radius = MathF.Max(0f, MathF.Min(radius, MathF.Min(width, height) / 2f));
        var left = x * _scale;
        var top = y * _scale;
        var w = width * _scale;
        var h = height * _scale;
        var r = radius * _scale;
        var minX = Math.Max(0, (int)MathF.Floor(left - 1f));
        var maxX = Math.Min(ScaledWidth - 1, (int)MathF.Ceiling(left + w + 1f));
        var minY = Math.Max(0, (int)MathF.Floor(top - 1f));
        var maxY = Math.Min(ScaledHeight - 1, (int)MathF.Ceiling(top + h + 1f));
        var halfW = w / 2f;
        var halfH = h / 2f;
        var centerX = left + halfW;
        var centerY = top + halfH;

        for (var py = minY; py <= maxY; py++)
        {
            var localY = py + 0.5f - centerY;
            for (var px = minX; px <= maxX; px++)
            {
                var localX = px + 0.5f - centerX;
                var qx = MathF.Abs(localX) - halfW + r;
                var qy = MathF.Abs(localY) - halfH + r;
                var outside = MathF.Sqrt((MathF.Max(qx, 0f) * MathF.Max(qx, 0f)) + (MathF.Max(qy, 0f) * MathF.Max(qy, 0f)));
                var inside = MathF.Min(MathF.Max(qx, qy), 0f);
                var distance = outside + inside - r;
                var coverage = Math.Clamp(0.5f - distance, 0f, 1f);
                if (coverage > 0f)
                {
                    Plot(px, py, color, coverage);
                }
            }
        }
    }

    public void FillCapsule(Vector2 a, Vector2 b, float radius, Color color)
    {
        var ax = a.X * _scale;
        var ay = a.Y * _scale;
        var bx = b.X * _scale;
        var by = b.Y * _scale;
        var r = radius * _scale;

        var minX = Math.Max(0, (int)MathF.Floor(MathF.Min(ax, bx) - r - 1f));
        var maxX = Math.Min(ScaledWidth - 1, (int)MathF.Ceiling(MathF.Max(ax, bx) + r + 1f));
        var minY = Math.Max(0, (int)MathF.Floor(MathF.Min(ay, by) - r - 1f));
        var maxY = Math.Min(ScaledHeight - 1, (int)MathF.Ceiling(MathF.Max(ay, by) + r + 1f));
        var dx = bx - ax;
        var dy = by - ay;
        var lengthSquared = (dx * dx) + (dy * dy);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var px = x + 0.5f - ax;
                var py = y + 0.5f - ay;
                float t = lengthSquared <= 0.0001f ? 0f : Math.Clamp(((px * dx) + (py * dy)) / lengthSquared, 0f, 1f);
                var closestX = dx * t;
                var closestY = dy * t;
                var ddx = px - closestX;
                var ddy = py - closestY;
                var distance = MathF.Sqrt((ddx * ddx) + (ddy * ddy));
                var coverage = Math.Clamp(r - distance + 0.5f, 0f, 1f);
                if (coverage > 0f)
                {
                    Plot(x, y, color, coverage);
                }
            }
        }
    }

    public void Stroke(Vector2 a, Vector2 b, float width, Color color)
        => FillCapsule(a, b, width / 2f, color);

    public void FillPolygon(IReadOnlyList<Vector2> points, Color color)
    {
        if (points.Count < 3)
        {
            return;
        }

        var scaled = points.Select(p => p * _scale).ToArray();
        var minY = Math.Max(0, (int)MathF.Floor(scaled.Min(p => p.Y)));
        var maxY = Math.Min(ScaledHeight - 1, (int)MathF.Ceiling(scaled.Max(p => p.Y)));

        for (var y = minY; y <= maxY; y++)
        {
            var scanY = y + 0.5f;
            var crossings = new List<float>();

            for (var i = 0; i < scaled.Length; i++)
            {
                var a = scaled[i];
                var b = scaled[(i + 1) % scaled.Length];
                if ((a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY))
                {
                    var t = (scanY - a.Y) / (b.Y - a.Y);
                    crossings.Add(a.X + (t * (b.X - a.X)));
                }
            }

            crossings.Sort();
            for (var i = 0; i + 1 < crossings.Count; i += 2)
            {
                var start = Math.Max(0, (int)MathF.Ceiling(crossings[i] - 0.5f));
                var end = Math.Min(ScaledWidth - 1, (int)MathF.Floor(crossings[i + 1] - 0.5f));
                for (var x = start; x <= end; x++)
                {
                    Plot(x, y, color, 1f);
                }
            }
        }
    }

    public void FillGlow(float centerX, float centerY, float radius, Color color)
    {
        var cx = centerX * _scale;
        var cy = centerY * _scale;
        var r = radius * _scale;
        var minX = Math.Max(0, (int)MathF.Floor(cx - r));
        var maxX = Math.Min(ScaledWidth - 1, (int)MathF.Ceiling(cx + r));
        var minY = Math.Max(0, (int)MathF.Floor(cy - r));
        var maxY = Math.Min(ScaledHeight - 1, (int)MathF.Ceiling(cy + r));

        for (var y = minY; y <= maxY; y++)
        {
            var dy = y + 0.5f - cy;
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x + 0.5f - cx;
                var distance = MathF.Sqrt((dx * dx) + (dy * dy)) / r;
                if (distance >= 1f)
                {
                    continue;
                }

                var falloff = 1f - distance;
                var coverage = falloff * falloff;
                Plot(x, y, color, coverage);
            }
        }
    }

    public void FillVignette(Color color, float innerRadius = 0.35f, float strength = 1f)
    {
        var centerX = ScaledWidth / 2f;
        var centerY = ScaledHeight / 2f;
        var maxDistance = MathF.Sqrt((centerX * centerX) + (centerY * centerY));

        for (var y = 0; y < ScaledHeight; y++)
        {
            var dy = y + 0.5f - centerY;
            for (var x = 0; x < ScaledWidth; x++)
            {
                var dx = x + 0.5f - centerX;
                var distance = MathF.Sqrt((dx * dx) + (dy * dy)) / maxDistance;
                var coverage = Math.Clamp((distance - innerRadius) / (1f - innerRadius), 0f, 1f);
                coverage = coverage * coverage * strength;
                if (coverage > 0f)
                {
                    Plot(x, y, color, coverage);
                }
            }
        }
    }

    public Texture2D CreateTexture(GraphicsDevice device)
    {
        var texture = new Texture2D(device, Width, Height, false, SurfaceFormat.Color);
        texture.SetData(Downsample());
        return texture;
    }

    private Color[] Downsample()
    {
        if (_scale == 1)
        {
            return (Color[])_pixels.Clone();
        }

        var result = new Color[Width * Height];
        var samples = _scale * _scale;

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var r = 0;
                var g = 0;
                var b = 0;
                var a = 0;

                for (var sy = 0; sy < _scale; sy++)
                {
                    var row = ((y * _scale) + sy) * ScaledWidth;
                    for (var sx = 0; sx < _scale; sx++)
                    {
                        var pixel = _pixels[row + (x * _scale) + sx];
                        r += pixel.R;
                        g += pixel.G;
                        b += pixel.B;
                        a += pixel.A;
                    }
                }

                result[(y * Width) + x] = new Color(r / samples, g / samples, b / samples, a / samples);
            }
        }

        return result;
    }

    private void Plot(int x, int y, Color color, float coverage)
    {
        if (x < 0 || y < 0 || x >= ScaledWidth || y >= ScaledHeight || coverage <= 0f)
        {
            return;
        }

        var alpha = (int)Math.Clamp(color.A * coverage, 0f, 255f);
        if (alpha == 0)
        {
            return;
        }

        var source = Color.FromNonPremultiplied(color.R, color.G, color.B, alpha);
        var index = (y * ScaledWidth) + x;
        var destination = _pixels[index];
        var inverse = 1f - (source.A / 255f);

        _pixels[index] = new Color(
            (int)Math.Clamp(source.R + (destination.R * inverse), 0f, 255f),
            (int)Math.Clamp(source.G + (destination.G * inverse), 0f, 255f),
            (int)Math.Clamp(source.B + (destination.B * inverse), 0f, 255f),
            (int)Math.Clamp(source.A + (destination.A * inverse), 0f, 255f));
    }
}
