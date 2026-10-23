using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mutants.Game.Rendering;

public static class UiPainter
{
    public static void Panel(
        SpriteBatch batch,
        Texture2D texture,
        Rectangle rect,
        Color fill,
        Color border,
        int borderWidth = 3,
        int shadow = 10)
    {
        if (shadow > 0)
        {
            batch.Draw(
                texture,
                new Rectangle(rect.X + (shadow / 2), rect.Y + shadow, rect.Width, rect.Height),
                new Color((byte)0, (byte)0, (byte)0, (byte)120));
        }

        if (border.A > 0 && borderWidth > 0)
        {
            batch.Draw(texture, rect, border);
        }

        var inset = Math.Max(0, borderWidth);
        var inner = new Rectangle(rect.X + inset, rect.Y + inset, Math.Max(1, rect.Width - (inset * 2)), Math.Max(1, rect.Height - (inset * 2)));
        batch.Draw(texture, inner, fill);
    }

    public static void Button(
        SpriteBatch batch,
        Texture2D texture,
        Rectangle rect,
        Color fill,
        Color border,
        bool selected = false,
        int borderWidth = 3)
    {
        Panel(batch, texture, rect, fill, selected ? ArtFactory.Lime : border, borderWidth, shadow: selected ? 12 : 8);

        if (selected)
        {
            var glow = new Rectangle(rect.X - 6, rect.Y - 6, rect.Width + 12, rect.Height + 12);
            batch.Draw(texture, glow, new Color((byte)182, (byte)242, (byte)107, (byte)38));
        }
    }

    public static void Bar(
        SpriteBatch batch,
        Texture2D texture,
        Rectangle rect,
        float fraction,
        Color fill,
        Color background,
        Color border)
    {
        Panel(batch, texture, rect, background, border, borderWidth: 2, shadow: 0);
        var inner = new Rectangle(rect.X + 4, rect.Y + 4, Math.Max(1, rect.Width - 8), Math.Max(1, rect.Height - 8));
        batch.Draw(texture, inner, ArtFactory.A(fill, 40));

        var width = (int)(inner.Width * Math.Clamp(fraction, 0f, 1f));
        if (width > 0)
        {
            var filled = new Rectangle(inner.X, inner.Y, width, inner.Height);
            batch.Draw(texture, filled, fill);
            batch.Draw(texture, new Rectangle(filled.X, filled.Y, filled.Width, Math.Max(2, filled.Height / 3)), ArtFactory.A(Color.White, 28));
        }
    }
}
