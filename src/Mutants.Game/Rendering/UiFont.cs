using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mutants.Game.Rendering;

public enum TextAlign
{
    Left,
    Center,
    Right,
}

/// <summary>
/// Loads an OFL font baked into an alpha atlas and draws it with clean linear
/// filtering. The atlas is loaded as straight alpha and must be drawn with
/// <see cref="BlendState.NonPremultiplied"/>.
/// </summary>
public sealed class UiFont : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FontData _data;
    private readonly Dictionary<char, Glyph> _glyphs;
    private readonly Texture2D _atlas;

    private UiFont(FontData data, Texture2D atlas)
    {
        _data = data;
        _atlas = atlas;
        _glyphs = data.Glyphs.ToDictionary(pair => pair.Key[0], pair => pair.Value);
    }

    public string Name => _data.Name;

    public float LineHeight => _data.LineHeight;

    public float Size => _data.Size;

    public static UiFont Load(GraphicsDevice device, string atlasResource, string metricsResource)
    {
        var assembly = typeof(UiFont).Assembly;

        using var metricsStream = assembly.GetManifestResourceStream(metricsResource)
            ?? throw new InvalidOperationException($"Missing font metrics resource '{metricsResource}'.");
        var data = JsonSerializer.Deserialize<FontData>(metricsStream, JsonOptions)
            ?? throw new InvalidOperationException($"Font metrics '{metricsResource}' are invalid.");

        using var textureStream = assembly.GetManifestResourceStream(atlasResource)
            ?? throw new InvalidOperationException($"Missing font atlas resource '{atlasResource}'.");

        var atlas = Texture2D.FromStream(device, textureStream);

        return new UiFont(data, atlas);
    }

    public Vector2 Measure(string text, float pixelSize)
    {
        var scale = pixelSize / _data.Size;
        var widest = 0f;
        var lineWidth = 0f;
        var lines = 1;

        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                widest = MathF.Max(widest, lineWidth);
                lineWidth = 0f;
                lines++;
                continue;
            }

            lineWidth += Advance(ch) * scale;
        }

        widest = MathF.Max(widest, lineWidth);
        return new Vector2(widest, lines * _data.LineHeight * scale);
    }

    public float MeasureLine(string text, float pixelSize)
    {
        var scale = pixelSize / _data.Size;
        var width = 0f;
        foreach (var ch in text)
        {
            width += Advance(ch) * scale;
        }

        return width;
    }

    public void Draw(
        SpriteBatch batch,
        string text,
        Vector2 position,
        float pixelSize,
        Color color,
        TextAlign align = TextAlign.Left,
        float alpha = 1f,
        bool shadow = false,
        Color? outline = null,
        float outlineWidth = 1.5f)
    {
        var scale = pixelSize / _data.Size;
        var lines = text.Split('\n');
        var lineHeight = _data.LineHeight * scale;
        var drawColor = new Color((byte)color.R, (byte)color.G, (byte)color.B, (byte)Math.Clamp(color.A * alpha, 0f, 255f));

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var width = MeasureLine(line, pixelSize);
            var x = align switch
            {
                TextAlign.Center => position.X - (width / 2f),
                TextAlign.Right => position.X - width,
                _ => position.X,
            };
            var y = position.Y + (lineIndex * lineHeight);

            if (shadow)
            {
                DrawLineRaw(batch, line, new Vector2(x + 3f, y + 4f), scale,
                    new Color((byte)2, (byte)2, (byte)6, (byte)Math.Clamp(200 * alpha, 0f, 255f)));
            }

            if (outline is { } outlineColor)
            {
                var outlineTint = new Color((byte)outlineColor.R, (byte)outlineColor.G, (byte)outlineColor.B,
                    (byte)Math.Clamp(outlineColor.A * alpha, 0f, 255f));

                DrawLineRaw(batch, line, new Vector2(x - outlineWidth, y), scale, outlineTint);
                DrawLineRaw(batch, line, new Vector2(x + outlineWidth, y), scale, outlineTint);
                DrawLineRaw(batch, line, new Vector2(x, y - outlineWidth), scale, outlineTint);
                DrawLineRaw(batch, line, new Vector2(x, y + outlineWidth), scale, outlineTint);
                DrawLineRaw(batch, line, new Vector2(x - outlineWidth, y - outlineWidth), scale, outlineTint);
                DrawLineRaw(batch, line, new Vector2(x + outlineWidth, y - outlineWidth), scale, outlineTint);
                DrawLineRaw(batch, line, new Vector2(x - outlineWidth, y + outlineWidth), scale, outlineTint);
                DrawLineRaw(batch, line, new Vector2(x + outlineWidth, y + outlineWidth), scale, outlineTint);
            }

            DrawLineRaw(batch, line, new Vector2(x, y), scale, drawColor);
        }
    }

    public void DrawCentered(
        SpriteBatch batch,
        string text,
        float centerX,
        float y,
        float pixelSize,
        Color color,
        float alpha = 1f,
        bool shadow = false,
        Color? outline = null,
        float outlineWidth = 1.5f)
        => Draw(batch, text, new Vector2(centerX, y), pixelSize, color, TextAlign.Center, alpha, shadow, outline, outlineWidth);

    private void DrawLineRaw(SpriteBatch batch, string text, Vector2 position, float scale, Color tint)
    {
        var penX = position.X;
        var penY = position.Y;

        foreach (var ch in text)
        {
            if (ch == ' ')
            {
                penX += Advance(ch) * scale;
                continue;
            }

            if (!_glyphs.TryGetValue(ch, out var glyph) || glyph.Width <= 0 || glyph.Height <= 0)
            {
                penX += Advance(ch) * scale;
                continue;
            }

            var destination = new Rectangle(
                (int)MathF.Round(penX + (glyph.OffsetX * scale)),
                (int)MathF.Round(penY + (glyph.OffsetY * scale)),
                (int)MathF.Ceiling(glyph.Width * scale),
                (int)MathF.Ceiling(glyph.Height * scale));

            var source = new Rectangle(glyph.X, glyph.Y, glyph.Width, glyph.Height);
            batch.Draw(_atlas, destination, source, tint);
            penX += glyph.Advance * scale;
        }
    }

    private float Advance(char ch)
        => _glyphs.TryGetValue(ch, out var glyph) ? glyph.Advance : _data.Size * 0.35f;

    public void Dispose() => _atlas.Dispose();

    private sealed class FontData
    {
        public string Name { get; set; } = "Font";

        public float Size { get; set; } = 64f;

        public float LineHeight { get; set; } = 80f;

        public float Ascent { get; set; } = 60f;

        public float Descent { get; set; } = 20f;

        public Dictionary<string, Glyph> Glyphs { get; set; } = new();
    }

    private sealed class Glyph
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("w")]
        public int Width { get; set; }

        [JsonPropertyName("h")]
        public int Height { get; set; }

        [JsonPropertyName("ox")]
        public float OffsetX { get; set; }

        [JsonPropertyName("oy")]
        public float OffsetY { get; set; }

        [JsonPropertyName("adv")]
        public float Advance { get; set; }
    }
}
