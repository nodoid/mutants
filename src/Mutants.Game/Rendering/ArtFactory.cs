using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mutants.Core;

namespace Mutants.Game.Rendering;

public sealed class ThemeArt
{
    public ThemeArt(
        Texture2D[] floors,
        Texture2D wallA,
        Texture2D wallB,
        Texture2D waterA,
        Texture2D waterB,
        Texture2D tree,
        Texture2D rock,
        Texture2D sky,
        Color fog,
        Color ambient,
        Color accent)
    {
        Floors = floors;
        WallA = wallA;
        WallB = wallB;
        WaterA = waterA;
        WaterB = waterB;
        Tree = tree;
        Rock = rock;
        Sky = sky;
        Fog = fog;
        Ambient = ambient;
        Accent = accent;
    }

    public Texture2D[] Floors { get; }

    public Texture2D WallA { get; }

    public Texture2D WallB { get; }

    public Texture2D WaterA { get; }

    public Texture2D WaterB { get; }

    public Texture2D Tree { get; }

    public Texture2D Rock { get; }

    public Texture2D Sky { get; }

    public Color Fog { get; }

    public Color Ambient { get; }

    public Color Accent { get; }
}

/// <summary>
/// Builds every sprite and tile at runtime from primitives. Nothing is copied
/// from the original game; the visual language is original spooky-arcade art.
/// </summary>
public sealed class ArtFactory : IDisposable
{
    public static readonly Color Ink = new(11, 7, 20);
    public static readonly Color Night = new(24, 16, 44);
    public static readonly Color Bone = new(240, 231, 204);
    public static readonly Color Lime = new(182, 242, 107);
    public static readonly Color Toxic = new(118, 205, 72);
    public static readonly Color Ember = new(255, 158, 61);
    public static readonly Color Blood = new(210, 65, 80);
    public static readonly Color Violet = new(122, 92, 255);
    public static readonly Color Moon = new(241, 232, 201);
    public static readonly Color Steel = new(166, 183, 196);
    public static readonly Color Gold = new(245, 196, 81);

    private readonly GraphicsDevice _device;
    private readonly List<Texture2D> _owned = new();

    public ArtFactory(GraphicsDevice device)
    {
        _device = device;

        Display = TrackFont(UiFont.Load(device, "Mutants.Game.Fonts.display.png", "Mutants.Game.Fonts.display.json"));
        Body = TrackFont(UiFont.Load(device, "Mutants.Game.Fonts.body.png", "Mutants.Game.Fonts.body.json"));

        using (var titleStream = GetResource("Mutants.Game.Art.title.png"))
        {
            Title = Track(Texture2D.FromStream(device, titleStream));
        }

        Glow = Track(Bake(128, 128, c => c.FillGlow(64, 64, 64, Color.White), supersample: 1));
        SoftGlow = Track(Bake(160, 160, c => c.FillGlow(80, 80, 80, A(Color.White, 180)), supersample: 1));
        Panel = Track(Bake(64, 64, c => c.FillRoundedRect(0, 0, 64, 64, 18, Color.White), supersample: 2));
        Shadow = Track(Bake(120, 56, c => c.FillEllipse(60, 28, 58, 26, A(Color.Black, 130)), supersample: 2));
        Fog = Track(BuildFog());
        Vignette = Track(BuildVignette());
        TitleBackdrop = Track(BuildTitleBackdrop());
        PauseIcon = Track(BuildPauseIcon());
        Skull = Track(BuildSkull());
        JoystickBase = Track(BuildJoystickBase());
        JoystickKnob = Track(Bake(120, 120, c =>
        {
            c.FillGlow(60, 60, 58, A(Color.White, 40));
            c.FillCircle(60, 60, 42, A(Color.White, 210));
            c.FillCircle(60, 60, 34, A(Color.White, 255));
            c.FillCircle(48, 46, 11, A(Color.White, 150));
        }, supersample: 2));

        PlayerA = Track(BuildPlayer(frame: 0));
        PlayerB = Track(BuildPlayer(frame: 1));
        CrawlerA = Track(BuildCrawler(frame: 0));
        CrawlerB = Track(BuildCrawler(frame: 1));
        BatA = Track(BuildBat(frame: 0));
        BatB = Track(BuildBat(frame: 1));
        GhoulA = Track(BuildGhoul(frame: 0));
        GhoulB = Track(BuildGhoul(frame: 1));
        Boss = Track(BuildBoss());
        Key = Track(BuildKey());
        Coin = Track(BuildCoin());
        Heart = Track(BuildHeart());
        Gate = Track(BuildGate());
        Portal = Track(BuildPortal());
        PlayerBolt = Track(BuildPlayerBolt());
        BossBolt = Track(BuildBossBolt());

        Themes = new Dictionary<LevelTheme, ThemeArt>
        {
            [LevelTheme.Graveyard] = BuildTheme(
                LevelTheme.Graveyard, "#0B0716", "#24313A", "#1B2C27", "#2C4036",
                "#3A3F3B", "#B6F26B", "#5A7464", "#14332F", seed: 101),
            [LevelTheme.Mire] = BuildTheme(
                LevelTheme.Mire, "#05100F", "#12332C", "#172E26", "#243F32",
                "#2C3B36", "#5FE0C0", "#3E6E5C", "#0C3B37", seed: 202),
            [LevelTheme.Foundry] = BuildTheme(
                LevelTheme.Foundry, "#100A0E", "#2C1712", "#2A2022", "#3A2B28",
                "#413336", "#FF9E3D", "#6A4132", "#3A2018", seed: 303),
            [LevelTheme.Chapel] = BuildTheme(
                LevelTheme.Chapel, "#070A18", "#22304E", "#232A3A", "#303C52",
                "#3A4258", "#9FD8FF", "#4E607D", "#1B2C4A", seed: 404),
            [LevelTheme.Clocktower] = BuildTheme(
                LevelTheme.Clocktower, "#10060B", "#36101A", "#2A1C20", "#3A262B",
                "#432B30", "#FF5A5F", "#6B3B44", "#2A1218", seed: 505),
        };
    }

    public UiFont Display { get; }

    public UiFont Body { get; }

    public Texture2D Title { get; }

    public Texture2D Glow { get; }

    public Texture2D SoftGlow { get; }

    public Texture2D Panel { get; }

    public Texture2D Shadow { get; }

    public Texture2D Fog { get; }

    public Texture2D Vignette { get; }

    public Texture2D TitleBackdrop { get; }

    public Texture2D PauseIcon { get; }

    public Texture2D Skull { get; }

    public Texture2D JoystickBase { get; }

    public Texture2D JoystickKnob { get; }

    public Texture2D PlayerA { get; }

    public Texture2D PlayerB { get; }

    public Texture2D CrawlerA { get; }

    public Texture2D CrawlerB { get; }

    public Texture2D BatA { get; }

    public Texture2D BatB { get; }

    public Texture2D GhoulA { get; }

    public Texture2D GhoulB { get; }

    public Texture2D Boss { get; }

    public Texture2D Key { get; }

    public Texture2D Coin { get; }

    public Texture2D Heart { get; }

    public Texture2D Gate { get; }

    public Texture2D Portal { get; }

    public Texture2D PlayerBolt { get; }

    public Texture2D BossBolt { get; }

    public IReadOnlyDictionary<LevelTheme, ThemeArt> Themes { get; }

    public static Color Hex(string value)
    {
        var hex = value.TrimStart('#');
        return new Color(
            Convert.ToInt32(hex[..2], 16),
            Convert.ToInt32(hex[2..4], 16),
            Convert.ToInt32(hex[4..6], 16));
    }

    public static Color A(Color color, int alpha) => new(color.R, color.G, color.B, Math.Clamp(alpha, 0, 255));

    public static Color Mix(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (int)(a.R + ((b.R - a.R) * t)),
            (int)(a.G + ((b.G - a.G) * t)),
            (int)(a.B + ((b.B - a.B) * t)),
            (int)(a.A + ((b.A - a.A) * t)));
    }

    public static Color Darken(Color color, float amount) => Mix(color, Color.Black, amount);

    public static Color Lighten(Color color, float amount) => Mix(color, Color.White, amount);

    private ThemeArt BuildTheme(
        LevelTheme theme,
        string skyTopHex,
        string skyBottomHex,
        string floorHex,
        string floorAltHex,
        string wallHex,
        string accentHex,
        string fogHex,
        string waterHex,
        int seed)
    {
        var skyTop = Hex(skyTopHex);
        var skyBottom = Hex(skyBottomHex);
        var floor = Hex(floorHex);
        var floorAlt = Hex(floorAltHex);
        var wall = Hex(wallHex);
        var accent = Hex(accentHex);
        var fog = Hex(fogHex);
        var water = Hex(waterHex);

        var floors = new Texture2D[4];
        for (var i = 0; i < floors.Length; i++)
        {
            var variant = i;
            floors[i] = Track(Bake(96, 96, c => DrawFloor(c, theme, floor, floorAlt, accent, variant, seed + (variant * 71)), supersample: 2));
        }

        var wallA = Track(Bake(96, 96, c => DrawWall(c, theme, wall, accent, seed + 7), supersample: 2));
        var wallB = Track(Bake(96, 96, c => DrawWall(c, theme, wall, accent, seed + 19), supersample: 2));
        var waterA = Track(Bake(96, 96, c => DrawWater(c, water, accent, 0), supersample: 2));
        var waterB = Track(Bake(96, 96, c => DrawWater(c, water, accent, 1), supersample: 2));
        var tree = Track(BuildTree(theme, wall, accent, seed));
        var rock = Track(BuildRock(theme, wall, accent, seed));
        var sky = Track(BuildSky(skyTop, skyBottom));

        return new ThemeArt(floors, wallA, wallB, waterA, waterB, tree, rock, sky, fog, accent, accent);
    }

    private Texture2D BuildSky(Color top, Color bottom)
        => Track(Bake(8, 720, c =>
        {
            for (var y = 0; y < 720; y++)
            {
                c.FillRect(0, y, 8, 1, Mix(top, bottom, y / 719f));
            }
        }, supersample: 1));

    private static void DrawFloor(
        Canvas c,
        LevelTheme theme,
        Color baseColor,
        Color altColor,
        Color accent,
        int variant,
        int seed)
    {
        var rng = new Random(seed);
        c.Fill(baseColor);

        for (var i = 0; i < 5; i++)
        {
            c.FillEllipse(
                rng.Next(8, 88),
                rng.Next(8, 88),
                rng.Next(8, 26),
                rng.Next(4, 14),
                A(rng.Next(2) == 0 ? altColor : Darken(baseColor, 0.3f), rng.Next(12, 30)));
        }

        for (var i = 0; i < 40; i++)
        {
            var x = rng.Next(2, 94);
            var y = rng.Next(2, 94);
            var radius = rng.Next(1, 3);
            var tone = rng.Next(3) switch
            {
                0 => A(Lighten(baseColor, 0.25f), rng.Next(20, 55)),
                1 => A(Darken(baseColor, 0.35f), rng.Next(30, 70)),
                _ => A(accent, rng.Next(10, 28)),
            };
            c.FillCircle(x, y, radius, tone);
        }

        switch (theme)
        {
            case LevelTheme.Graveyard:
                for (var i = 0; i < 7; i++)
                {
                    var x = rng.Next(10, 86);
                    var y = rng.Next(10, 86);
                    c.Stroke(new Vector2(x, y), new Vector2(x - 2, y - 7), 1.2f, A(Toxic, 70));
                    c.Stroke(new Vector2(x, y), new Vector2(x + 3, y - 6), 1.2f, A(Toxic, 50));
                }

                c.FillCircle(rng.Next(12, 84), rng.Next(12, 84), 3.5f, A(Bone, 35));
                break;

            case LevelTheme.Mire:
                c.FillEllipse(24 + variant * 9, 70 - variant * 7, 20, 9, A(accent, 26));
                c.FillEllipse(70, 24 + variant * 8, 16, 7, A(accent, 20));
                for (var i = 0; i < 5; i++)
                {
                    var x = rng.Next(5, 90);
                    c.Stroke(new Vector2(x, 90), new Vector2(x + rng.Next(-4, 5), 76), 1f, A(Toxic, 55));
                }

                break;

            case LevelTheme.Foundry:
                c.Stroke(new Vector2(0, 24 + variant * 3), new Vector2(96, 24 + variant * 3), 1f, A(Darken(baseColor, 0.45f), 120));
                c.Stroke(new Vector2(48 + variant * 2, 0), new Vector2(48 + variant * 2, 96), 1f, A(Darken(baseColor, 0.4f), 90));
                for (var i = 0; i < 4; i++)
                {
                    c.FillCircle(12 + i * 24, 12 + ((i + variant) % 2) * 72, 2.4f, A(Steel, 65));
                }

                c.FillRect(rng.Next(18, 70), rng.Next(30, 70), rng.Next(10, 22), rng.Next(3, 7), A(Ember, 22));
                break;

            case LevelTheme.Chapel:
                c.Stroke(new Vector2(0, 48), new Vector2(96, 48), 1.2f, A(Darken(baseColor, 0.45f), 100));
                c.Stroke(new Vector2(48, 0), new Vector2(48, 96), 1.2f, A(Darken(baseColor, 0.4f), 80));
                c.FillEllipse(rng.Next(10, 86), rng.Next(10, 86), rng.Next(8, 18), rng.Next(2, 5), A(accent, 22));
                for (var i = 0; i < 3; i++)
                {
                    var x = rng.Next(12, 84);
                    var y = rng.Next(12, 84);
                    c.FillCircle(x, y, 1.4f, A(Color.White, 80));
                }

                break;

            case LevelTheme.Clocktower:
                c.Stroke(new Vector2(0, 32), new Vector2(96, 32), 1.2f, A(Darken(baseColor, 0.55f), 120));
                c.Stroke(new Vector2(0, 64), new Vector2(96, 64), 1.2f, A(Darken(baseColor, 0.5f), 100));
                c.FillCircle(48, 48, 22 + variant * 2, A(accent, 18));
                c.FillCircle(48, 48, 2.5f, A(Ember, 70));
                for (var i = 0; i < 4; i++)
                {
                    c.Stroke(
                        new Vector2(48, 48),
                        new Vector2(48 + MathF.Cos(i * 1.57f) * 20, 48 + MathF.Sin(i * 1.57f) * 20),
                        1.2f,
                        A(Ember, 50));
                }

                break;
        }

        c.Stroke(new Vector2(0, 95), new Vector2(96, 95), 1f, A(Color.Black, 28));
        c.Stroke(new Vector2(95, 0), new Vector2(95, 96), 1f, A(Color.Black, 22));
    }

    private static void DrawWall(Canvas c, LevelTheme theme, Color wall, Color accent, int seed)
    {
        var rng = new Random(seed);
        c.Fill(Darken(wall, 0.12f));

        var brickWidth = theme == LevelTheme.Clocktower ? 32 : 48;
        var brickHeight = 24;
        for (var row = 0; row < 4; row++)
        {
            var offset = row % 2 == 0 ? 0 : brickWidth / 2;
            for (var col = -1; col < 3; col++)
            {
                var x = (col * brickWidth) + offset;
                var shade = rng.Next(-10, 11);
                var brick = new Color(
                    (byte)Math.Clamp(wall.R + shade, 0, 255),
                    (byte)Math.Clamp(wall.G + shade, 0, 255),
                    (byte)Math.Clamp(wall.B + shade, 0, 255),
                    (byte)255);
                c.FillRoundedRect(x + 2, row * brickHeight + 2, brickWidth - 4, brickHeight - 4, 2, brick);
                c.Stroke(new Vector2(x + 4, row * brickHeight + 4), new Vector2(x + brickWidth - 5, row * brickHeight + 4), 1.4f, A(Lighten(wall, 0.22f), 90));
                c.Stroke(new Vector2(x + 3, row * brickHeight + 4), new Vector2(x + 3, row * brickHeight + brickHeight - 5), 1f, A(Color.Black, 40));
            }
        }

        if (theme == LevelTheme.Foundry)
        {
            c.FillCircle(12, 12, 3, A(Steel, 90));
            c.FillCircle(84, 84, 3, A(Steel, 80));
            c.FillRect(20, 50, 28, 4, A(Ember, 70));
        }
        else if (theme == LevelTheme.Chapel)
        {
            c.FillCircle(48, 48, 18, A(accent, 20));
            c.Stroke(new Vector2(48, 28), new Vector2(48, 68), 2f, A(accent, 60));
            c.Stroke(new Vector2(30, 48), new Vector2(66, 48), 2f, A(accent, 60));
        }
        else if (theme == LevelTheme.Clocktower)
        {
            c.FillCircle(48, 48, 26, A(Color.Black, 24));
            c.FillCircle(48, 48, 22, A(Ember, 18));
        }
        else if (theme == LevelTheme.Mire)
        {
            c.FillEllipse(20, 18, 14, 5, A(Toxic, 30));
            c.FillEllipse(70, 74, 16, 6, A(Toxic, 24));
        }

        c.FillRect(0, 0, 96, 4, A(Lighten(wall, 0.3f), 180));
        c.FillRect(0, 92, 96, 4, A(Color.Black, 100));
        c.FillRect(92, 0, 4, 96, A(Color.Black, 60));
    }

    private static void DrawWater(Canvas c, Color water, Color accent, int frame)
    {
        c.Fill(water);
        for (var i = 0; i < 5; i++)
        {
            var y = 12 + (i * 18) + (frame * 4);
            var points = new List<Vector2>();
            for (var x = 0; x <= 96; x += 8)
            {
                points.Add(new Vector2(x, y + (MathF.Sin((x * 0.09f) + (i * 1.2f) + (frame * 0.7f)) * 3f)));
            }

            for (var p = 0; p + 1 < points.Count; p++)
            {
                c.Stroke(points[p], points[p + 1], 1.4f, A(accent, 42));
            }
        }

        c.FillEllipse(30, 30 + frame * 6, 22, 6, A(accent, 18));
        c.FillEllipse(70, 64 - frame * 4, 16, 5, A(accent, 14));
    }

    private Texture2D BuildTree(LevelTheme theme, Color wall, Color accent, int seed)
    {
        var rng = new Random(seed);
        return Track(Bake(150, 220, c =>
        {
            var bark = theme == LevelTheme.Foundry ? Darken(wall, 0.35f) : darkBark(theme, wall);
            c.FillEllipse(74, 207, 44, 11, A(Color.Black, 110));
            c.FillCapsule(new Vector2(78, 210), new Vector2(74, 108), 13, bark);
            c.FillCapsule(new Vector2(70, 132), new Vector2(44, 80), 7, bark);
            c.FillCapsule(new Vector2(80, 126), new Vector2(110, 72), 6.5f, bark);
            c.FillCapsule(new Vector2(46, 84), new Vector2(26, 48), 4.5f, bark);
            c.FillCapsule(new Vector2(48, 82), new Vector2(66, 38), 4f, bark);
            c.FillCapsule(new Vector2(108, 76), new Vector2(128, 42), 4f, bark);
            c.FillCapsule(new Vector2(106, 78), new Vector2(92, 34), 3.4f, bark);
            c.FillCapsule(new Vector2(70, 50), new Vector2(52, 18), 3f, bark);
            c.FillCapsule(new Vector2(96, 44), new Vector2(112, 16), 2.6f, bark);

            for (var i = 0; i < 9; i++)
            {
                var x = rng.Next(55, 105);
                var y = rng.Next(95, 200);
                c.Stroke(new Vector2(x, y), new Vector2(x + rng.Next(-2, 3), y + 8), 1.2f, A(Darken(bark, 0.35f), 120));
            }

            if (theme is LevelTheme.Mire or LevelTheme.Graveyard)
            {
                c.FillEllipse(62, 104, 18, 7, A(Toxic, 40));
                c.FillEllipse(80, 150, 16, 6, A(accent, 30));
            }
            else if (theme == LevelTheme.Chapel)
            {
                c.FillEllipse(64, 104, 18, 6, A(accent, 45));
                c.FillCircle(118, 44, 4, A(accent, 45));
            }

            c.FillCapsule(new Vector2(70, 208), new Vector2(52, 214), 4, bark);
            c.FillCapsule(new Vector2(78, 208), new Vector2(102, 216), 4, bark);
        }));
    }

    private static Color darkBark(LevelTheme theme, Color wall)
        => theme switch
        {
            LevelTheme.Graveyard => Hex("#2A2721"),
            LevelTheme.Mire => Hex("#202A20"),
            LevelTheme.Foundry => Hex("#2A2220"),
            LevelTheme.Chapel => Hex("#232838"),
            _ => Hex("#251A1C"),
        };

    private Texture2D BuildRock(LevelTheme theme, Color wall, Color accent, int seed)
    {
        return Track(Bake(120, 100, c =>
        {
            var baseColor = theme == LevelTheme.Foundry ? Hex("#3D3334") : Darken(wall, 0.05f);
            c.FillEllipse(60, 86, 44, 9, A(Color.Black, 120));
            c.FillPolygon(new[]
            {
                new Vector2(16, 88), new Vector2(22, 48), new Vector2(42, 20),
                new Vector2(78, 16), new Vector2(102, 46), new Vector2(104, 86),
            }, Darken(baseColor, 0.3f));
            c.FillPolygon(new[]
            {
                new Vector2(22, 80), new Vector2(30, 44), new Vector2(50, 26),
                new Vector2(76, 24), new Vector2(94, 48), new Vector2(92, 80),
            }, baseColor);
            c.FillPolygon(new[]
            {
                new Vector2(32, 44), new Vector2(52, 30), new Vector2(72, 34), new Vector2(58, 50),
            }, A(Lighten(baseColor, 0.28f), 90));
            c.Stroke(new Vector2(46, 26), new Vector2(56, 56), 1.5f, A(Color.Black, 80));
            c.Stroke(new Vector2(56, 56), new Vector2(78, 72), 1.2f, A(Color.Black, 60));
            c.FillCircle(30, 70, 5, A(Mix(baseColor, accent, 0.4f), 40));
        }));
    }

    private Texture2D BuildPlayer(int frame)
    {
        return Track(Bake(120, 150, c =>
        {
            var cloak = Hex("#27234A");
            var cloakLight = Hex("#3A3568");
            var outline = Hex("#0B0714");
            var skin = Hex("#E3C6A6");
            var boot = Hex("#17121F");

            // Legs / boots.
            var left = frame == 0 ? new Vector2(52, 130) : new Vector2(58, 132);
            var right = frame == 0 ? new Vector2(70, 132) : new Vector2(64, 130);
            c.FillCapsule(left, new Vector2(left.X - 2, 143), 7, boot);
            c.FillCapsule(right, new Vector2(right.X + 2, 143), 7, boot);
            c.FillEllipse(left.X - 3, 143, 12, 5, outline);
            c.FillEllipse(right.X + 4, 143, 12, 5, outline);

            // Back arm.
            c.FillCapsule(new Vector2(48, 76), new Vector2(34, 102), 9, outline);
            c.FillCapsule(new Vector2(48, 76), new Vector2(36, 101), 6, cloakLight);

            // Cloak body.
            c.FillPolygon(new[]
            {
                new Vector2(32, 142), new Vector2(40, 80), new Vector2(52, 60),
                new Vector2(74, 64), new Vector2(84, 86), new Vector2(92, 142),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(36, 140), new Vector2(43, 82), new Vector2(54, 64),
                new Vector2(72, 68), new Vector2(81, 88), new Vector2(88, 140),
            }, cloak);
            c.FillPolygon(new[]
            {
                new Vector2(40, 136), new Vector2(46, 86), new Vector2(54, 68),
                new Vector2(58, 72), new Vector2(51, 106), new Vector2(48, 138),
            }, A(cloakLight, 160));
            c.Stroke(new Vector2(44, 104), new Vector2(84, 108), 3f, Hex("#171326"));

            // Hood.
            c.FillEllipse(60, 52, 27, 28, outline);
            c.FillEllipse(60, 53, 24, 25, cloak);
            c.FillPolygon(new[]
            {
                new Vector2(40, 48), new Vector2(58, 20), new Vector2(66, 28),
                new Vector2(76, 38), new Vector2(74, 52), new Vector2(62, 40), new Vector2(48, 54),
            }, cloak);
            c.FillEllipse(63, 56, 14, 16, skin);
            c.FillEllipse(68, 53, 3.2f, 3.6f, Hex("#121018"));
            c.FillEllipse(56, 53, 2.6f, 3.2f, Hex("#121018"));
            c.Stroke(new Vector2(62, 62), new Vector2(68, 62), 1.4f, A(Hex("#9E715A"), 160));

            // Scarf.
            c.FillCapsule(new Vector2(42, 70), new Vector2(68, 73), 5.5f, outline);
            c.FillCapsule(new Vector2(43, 69), new Vector2(67, 72), 4.2f, Lime);
            c.FillPolygon(new[]
            {
                new Vector2(44, 70), new Vector2(36, 82), new Vector2(42, 86), new Vector2(54, 73),
            }, A(Lime, 220));

            // Front arm.
            c.FillCapsule(new Vector2(66, 78), new Vector2(88, 96), 7.5f, outline);
            c.FillCapsule(new Vector2(66, 78), new Vector2(86, 95), 5.5f, cloakLight);
            c.FillCircle(90, 98, 5, skin);

            // Lantern.
            c.Stroke(new Vector2(90, 98), new Vector2(96, 108), 1.6f, Hex("#5A4B73"));
            c.FillRoundedRect(87, 106, 22, 26, 5, outline);
            c.FillRoundedRect(89, 108, 18, 22, 4, Hex("#3A2C4C"));
            c.FillRoundedRect(92, 111, 12, 14, 2, Hex("#FFC46B"));
            c.FillCircle(98, 118, 4.5f, Hex("#FFF3C4"));
            c.FillCircle(98, 118, 2f, Color.White);
            c.FillRect(94, 105, 8, 2, A(Ember, 140));

            // Rim light.
            c.Stroke(new Vector2(36, 138), new Vector2(43, 84), 2f, A(Hex("#7D8CFF"), 50));
        }));
    }

    private Texture2D BuildCrawler(int frame)
    {
        return Track(Bake(110, 90, c =>
        {
            var body = Hex("#5E8F3A");
            var bodyLight = Hex("#8CC24F");
            var outline = Hex("#10180C");
            var eye = Hex("#F5F06A");
            var legY = frame == 0 ? 76 : 74;

            for (var i = 0; i < 3; i++)
            {
                var x = 24 + (i * 22);
                c.Stroke(new Vector2(x, 60), new Vector2(x - 12, legY), 5, outline);
                c.Stroke(new Vector2(x + 10, 62), new Vector2(x + 24, legY - 2), 5, outline);
            }

            c.FillEllipse(54, 54, 38, 25, outline);
            c.FillEllipse(54, 52, 35, 22, body);
            c.FillEllipse(48, 46, 22, 12, A(bodyLight, 170));
            c.FillPolygon(new[]
            {
                new Vector2(34, 38), new Vector2(42, 18), new Vector2(50, 36),
                new Vector2(58, 16), new Vector2(64, 36), new Vector2(74, 22), new Vector2(76, 40),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(38, 36), new Vector2(43, 24), new Vector2(48, 36),
                new Vector2(58, 22), new Vector2(63, 36), new Vector2(72, 28), new Vector2(72, 38),
            }, Darken(body, 0.22f));
            c.FillCircle(66, 48, 6, A(Color.Black, 130));
            c.FillCircle(66, 48, 3.4f, eye);
            c.FillCircle(67, 47, 1.2f, Color.White);
            c.FillCircle(52, 50, 4.2f, A(Color.Black, 120));
            c.FillCircle(52, 50, 2.2f, eye);
            c.Stroke(new Vector2(58, 66), new Vector2(78, 72), 2f, outline);
            c.FillPolygon(new[]
            {
                new Vector2(74, 66), new Vector2(82, 70), new Vector2(76, 74),
            }, Bone);
            c.FillPolygon(new[]
            {
                new Vector2(84, 66), new Vector2(92, 70), new Vector2(84, 74),
            }, Bone);
        }));
    }

    private Texture2D BuildBat(int frame)
    {
        return Track(Bake(130, 90, c =>
        {
            var wing = Hex("#4B2A68");
            var wingLight = Hex("#70408F");
            var body = Hex("#2E1B43");
            var outline = Hex("#0D0715");
            var wingLift = frame == 0 ? -12f : 8f;

            for (var side = -1; side <= 1; side += 2)
            {
                var root = new Vector2(65 + (side * 12), 48);
                c.FillPolygon(new[]
                {
                    root,
                    new Vector2(65 + (side * 30), 26 + wingLift),
                    new Vector2(65 + (side * 52), 20 + wingLift),
                    new Vector2(65 + (side * 48), 46),
                    new Vector2(65 + (side * 58), 58 + (wingLift * 0.4f)),
                    new Vector2(65 + (side * 36), 54),
                    new Vector2(65 + (side * 30), 66),
                }, outline);
                c.FillPolygon(new[]
                {
                    root,
                    new Vector2(65 + (side * 30), 29 + wingLift),
                    new Vector2(65 + (side * 48), 24 + wingLift),
                    new Vector2(65 + (side * 44), 44),
                    new Vector2(65 + (side * 52), 54 + (wingLift * 0.4f)),
                    new Vector2(65 + (side * 34), 51),
                    new Vector2(65 + (side * 29), 61),
                }, wing);
                c.Stroke(new Vector2(65 + (side * 20), 44), new Vector2(65 + (side * 42), 36 + wingLift), 1.4f, A(wingLight, 150));
            }

            c.FillEllipse(65, 52, 15, 20, outline);
            c.FillEllipse(65, 51, 12, 17, body);
            c.FillPolygon(new[]
            {
                new Vector2(56, 38), new Vector2(58, 20), new Vector2(65, 34),
                new Vector2(72, 20), new Vector2(74, 38),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(59, 36), new Vector2(60, 25), new Vector2(65, 35),
                new Vector2(70, 25), new Vector2(71, 36),
            }, body);
            c.FillCircle(60, 48, 3, Hex("#FF6B6B"));
            c.FillCircle(70, 48, 3, Hex("#FF6B6B"));
            c.FillCircle(60, 48, 1.2f, Color.White);
            c.FillCircle(70, 48, 1.2f, Color.White);
            c.FillPolygon(new[]
            {
                new Vector2(61, 62), new Vector2(63, 70), new Vector2(65, 62),
                new Vector2(67, 70), new Vector2(69, 62),
            }, Bone);
        }));
    }

    private Texture2D BuildGhoul(int frame)
    {
        return Track(Bake(120, 160, c =>
        {
            var robe = Hex("#4A4C5E");
            var robeLight = Hex("#6A6C80");
            var outline = Hex("#101018");
            var skin = Hex("#C7C0AC");
            var armSwing = frame == 0 ? 0f : 6f;

            // Arms.
            c.FillCapsule(new Vector2(36, 70), new Vector2(22, 112 + armSwing), 8, outline);
            c.FillCapsule(new Vector2(36, 70), new Vector2(24, 110 + armSwing), 5.5f, skin);
            c.FillCapsule(new Vector2(84, 70), new Vector2(98, 112 - armSwing), 8, outline);
            c.FillCapsule(new Vector2(84, 70), new Vector2(96, 110 - armSwing), 5.5f, skin);
            for (var i = -1; i <= 1; i++)
            {
                c.Stroke(new Vector2(96 + (i * 4), 110 - armSwing), new Vector2(100 + (i * 6), 120 - armSwing), 2f, outline);
            }

            // Tattered robe.
            c.FillPolygon(new[]
            {
                new Vector2(34, 152), new Vector2(38, 80), new Vector2(48, 54),
                new Vector2(76, 56), new Vector2(86, 86), new Vector2(92, 152),
                new Vector2(80, 142), new Vector2(72, 154), new Vector2(62, 142),
                new Vector2(52, 156), new Vector2(44, 142),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(38, 148), new Vector2(42, 82), new Vector2(50, 58),
                new Vector2(74, 60), new Vector2(82, 88), new Vector2(87, 148),
                new Vector2(79, 138), new Vector2(72, 150), new Vector2(63, 138),
                new Vector2(54, 150), new Vector2(45, 138),
            }, robe);
            c.FillPolygon(new[]
            {
                new Vector2(46, 80), new Vector2(50, 62), new Vector2(60, 72), new Vector2(54, 128),
            }, A(robeLight, 80));
            c.Stroke(new Vector2(48, 112), new Vector2(82, 116), 2f, A(outline, 140));

            // Head.
            c.FillEllipse(60, 48, 24, 25, outline);
            c.FillEllipse(60, 48, 21, 22, skin);
            c.FillEllipse(60, 54, 16, 13, Darken(skin, 0.18f));
            c.FillEllipse(52, 46, 6, 5, Hex("#17131C"));
            c.FillEllipse(68, 46, 6, 5, Hex("#17131C"));
            c.FillCircle(52, 47, 2.8f, Hex("#FFB347"));
            c.FillCircle(68, 47, 2.8f, Hex("#FFB347"));
            c.FillCircle(52, 47, 1.1f, Color.White);
            c.FillCircle(68, 47, 1.1f, Color.White);
            c.Stroke(new Vector2(58, 60), new Vector2(62, 60), 3f, Hex("#201924"));
            for (var i = 0; i < 4; i++)
            {
                c.FillRect(53 + (i * 4), 62, 2, 5, Bone);
            }

            c.FillEllipse(60, 156, 36, 6, A(Color.Black, 90));
        }));
    }

    private Texture2D BuildBoss()
    {
        return Track(Bake(240, 260, c =>
        {
            var cloak = Hex("#5A1424");
            var cloakDark = Hex("#340C16");
            var armour = Hex("#2A2338");
            var outline = Hex("#08040F");
            var bone = Hex("#D9CFB4");

            c.FillEllipse(120, 248, 92, 13, A(Color.Black, 130));

            // Cape.
            c.FillPolygon(new[]
            {
                new Vector2(56, 250), new Vector2(34, 128), new Vector2(58, 84),
                new Vector2(182, 84), new Vector2(206, 128), new Vector2(184, 250),
                new Vector2(160, 232), new Vector2(146, 250), new Vector2(132, 228),
                new Vector2(114, 250), new Vector2(96, 228), new Vector2(80, 252),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(62, 244), new Vector2(42, 130), new Vector2(64, 92),
                new Vector2(176, 92), new Vector2(198, 130), new Vector2(178, 244),
                new Vector2(158, 226), new Vector2(144, 244), new Vector2(130, 222),
                new Vector2(112, 244), new Vector2(94, 222), new Vector2(78, 246),
            }, cloak);
            c.FillPolygon(new[]
            {
                new Vector2(62, 244), new Vector2(42, 130), new Vector2(64, 92),
                new Vector2(80, 108), new Vector2(86, 210),
            }, A(cloakDark, 200));

            // Body armour.
            c.FillEllipse(120, 160, 56, 66, outline);
            c.FillEllipse(120, 158, 50, 60, armour);
            c.FillEllipse(120, 146, 32, 30, A(Steel, 15));
            c.FillPolygon(new[]
            {
                new Vector2(96, 132), new Vector2(120, 112), new Vector2(144, 132),
                new Vector2(120, 166),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(99, 132), new Vector2(120, 116), new Vector2(141, 132),
                new Vector2(120, 160),
            }, A(Hex("#8B1A2A"), 255));
            c.FillCircle(120, 136, 9, A(Hex("#FF5A5F"), 200));
            c.FillCircle(120, 136, 4, A(Hex("#FFE1A6"), 220));

            // Arms and claws.
            c.FillCapsule(new Vector2(70, 128), new Vector2(40, 200), 17, outline);
            c.FillCapsule(new Vector2(70, 128), new Vector2(44, 196), 12, armour);
            c.FillCapsule(new Vector2(170, 128), new Vector2(202, 200), 17, outline);
            c.FillCapsule(new Vector2(170, 128), new Vector2(198, 196), 12, armour);
            for (var i = -1; i <= 1; i++)
            {
                c.FillPolygon(new[]
                {
                    new Vector2(42 + (i * 8), 196), new Vector2(34 + (i * 10), 222), new Vector2(50 + (i * 7), 205),
                }, bone);
                c.FillPolygon(new[]
                {
                    new Vector2(198 + (i * 8), 196), new Vector2(206 + (i * 10), 222), new Vector2(190 + (i * 7), 205),
                }, bone);
            }

            // Horns and crown.
            c.FillPolygon(new[]
            {
                new Vector2(76, 76), new Vector2(48, 28), new Vector2(96, 58),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(164, 76), new Vector2(192, 28), new Vector2(144, 58),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(82, 72), new Vector2(58, 38), new Vector2(94, 60),
            }, bone);
            c.FillPolygon(new[]
            {
                new Vector2(158, 72), new Vector2(182, 38), new Vector2(146, 60),
            }, bone);

            c.FillPolygon(new[]
            {
                new Vector2(86, 60), new Vector2(94, 28), new Vector2(106, 48),
                new Vector2(120, 20), new Vector2(134, 48), new Vector2(146, 28),
                new Vector2(154, 60),
            }, outline);
            c.FillPolygon(new[]
            {
                new Vector2(90, 58), new Vector2(96, 34), new Vector2(107, 50),
                new Vector2(120, 26), new Vector2(133, 50), new Vector2(144, 34),
                new Vector2(150, 58),
            }, Gold);

            // Skull face.
            c.FillEllipse(120, 82, 36, 38, outline);
            c.FillEllipse(120, 82, 32, 34, bone);
            c.FillEllipse(106, 80, 9, 11, Hex("#120D16"));
            c.FillEllipse(134, 80, 9, 11, Hex("#120D16"));
            c.FillCircle(106, 81, 4, Hex("#FF5A5F"));
            c.FillCircle(134, 81, 4, Hex("#FF5A5F"));
            c.FillCircle(106, 81, 1.6f, Color.White);
            c.FillCircle(134, 81, 1.6f, Color.White);
            c.FillPolygon(new[]
            {
                new Vector2(116, 94), new Vector2(124, 94), new Vector2(122, 102), new Vector2(118, 102),
            }, Hex("#120D16"));
            for (var i = 0; i < 5; i++)
            {
                c.FillRect(106 + (i * 6), 106, 4, 7, A(bone, 255));
            }

            // Rim light.
            c.Stroke(new Vector2(66, 240), new Vector2(48, 132), 3f, A(Violet, 40));
            c.Stroke(new Vector2(174, 240), new Vector2(194, 132), 2f, A(Ember, 40));
        }));
    }

    private Texture2D BuildKey()
    {
        return Track(Bake(60, 60, c =>
        {
            var outline = Hex("#2A1B06");
            var gold = Gold;
            for (var i = 0; i < 34; i++)
            {
                var angle = i / 34f * MathF.Tau;
                c.FillCircle(20 + (MathF.Cos(angle) * 11), 20 + (MathF.Sin(angle) * 11), 5, outline);
            }

            for (var i = 0; i < 34; i++)
            {
                var angle = i / 34f * MathF.Tau;
                c.FillCircle(20 + (MathF.Cos(angle) * 11), 20 + (MathF.Sin(angle) * 11), 3.6f, gold);
            }

            c.FillCapsule(new Vector2(30, 24), new Vector2(52, 42), 4.2f, outline);
            c.FillCapsule(new Vector2(30, 24), new Vector2(51, 41), 2.8f, gold);
            c.FillRoundedRect(46, 38, 7, 8, 1.5f, outline);
            c.FillRoundedRect(47, 39, 5, 5, 1f, gold);
            c.FillRoundedRect(50, 46, 6, 6, 1.5f, gold);
            c.FillCircle(17, 17, 3, Color.White);
            c.Stroke(new Vector2(14, 15), new Vector2(24, 11), 1.4f, A(Color.White, 160));
        }));
    }

    private Texture2D BuildCoin()
    {
        return Track(Bake(44, 44, c =>
        {
            var dark = Hex("#3E4650");
            c.FillCircle(22, 23, 20, A(Color.Black, 70));
            c.FillCircle(22, 21, 19, dark);
            c.FillCircle(22, 21, 16, Steel);
            c.FillCircle(22, 21, 13, Hex("#CBD6DE"));
            c.FillEllipse(22, 22, 7, 8, A(dark, 210));
            c.FillEllipse(19, 20, 2, 2.4f, A(Color.White, 200));
            c.FillEllipse(25, 20, 2, 2.4f, A(Color.White, 200));
            c.Stroke(new Vector2(18, 26), new Vector2(26, 26), 1.5f, A(dark, 180));
            c.FillRect(21, 26, 2, 4, A(dark, 180));
            c.Stroke(new Vector2(10, 14), new Vector2(18, 8), 1.6f, A(Color.White, 120));
        }));
    }

    private Texture2D BuildHeart()
    {
        return Track(Bake(48, 44, c =>
        {
            var dark = Hex("#3A0B16");
            c.FillCircle(16, 18, 12, dark);
            c.FillCircle(32, 18, 12, dark);
            c.FillPolygon(new[]
            {
                new Vector2(5, 20), new Vector2(43, 20), new Vector2(24, 41),
            }, dark);
            c.FillCircle(16, 17, 9.5f, Blood);
            c.FillCircle(32, 17, 9.5f, Blood);
            c.FillPolygon(new[]
            {
                new Vector2(7, 19), new Vector2(41, 19), new Vector2(24, 38),
            }, Blood);
            c.FillEllipse(16, 13, 4, 3, A(Color.White, 160));
            c.FillCircle(12, 20, 2, A(Color.White, 60));
        }));
    }

    private Texture2D BuildGate()
    {
        return Track(Bake(120, 190, c =>
        {
            var outline = Hex("#0C0A10");
            var iron = Hex("#3B3F4A");
            var highlight = Hex("#6B7180");

            c.FillEllipse(60, 182, 52, 9, A(Color.Black, 120));
            c.FillRoundedRect(10, 20, 14, 162, 4, outline);
            c.FillRoundedRect(96, 20, 14, 162, 4, outline);
            c.FillRoundedRect(12, 22, 10, 158, 3, iron);
            c.FillRoundedRect(98, 22, 10, 158, 3, iron);
            c.FillCircle(17, 18, 9, outline);
            c.FillCircle(103, 18, 9, outline);
            c.FillCircle(17, 18, 6, iron);
            c.FillCircle(103, 18, 6, iron);

            for (var i = 0; i < 6; i++)
            {
                var x = 30 + (i * 11);
                c.FillRect(x, 40, 6, 132, outline);
                c.FillRect(x + 1, 42, 3, 128, iron);
                c.FillPolygon(new[]
                {
                    new Vector2(x - 2, 42), new Vector2(x + 3, 24), new Vector2(x + 8, 42),
                }, outline);
                c.FillPolygon(new[]
                {
                    new Vector2(x, 41), new Vector2(x + 3, 28), new Vector2(x + 6, 41),
                }, highlight);
                c.FillCircle(x + 3, 78, 3.5f, A(highlight, 100));
                c.FillCircle(x + 3, 120, 3.5f, A(highlight, 80));
            }

            c.FillRect(22, 68, 76, 7, outline);
            c.FillRect(22, 70, 76, 3, iron);
            c.FillRect(22, 122, 76, 7, outline);
            c.FillRect(22, 124, 76, 3, iron);

            c.FillCircle(60, 100, 13, outline);
            c.FillCircle(60, 100, 10, Hex("#2A2C34"));
            c.FillCircle(60, 100, 5, A(Ember, 160));
            c.FillRect(58, 100, 4, 14, Hex("#2A2C34"));

            c.Stroke(new Vector2(10, 34), new Vector2(110, 34), 1.2f, A(highlight, 60));
        }));
    }

    private Texture2D BuildPortal()
    {
        return Track(Bake(220, 250, c =>
        {
            c.FillGlow(110, 150, 108, A(Violet, 40));
            for (var layer = 0; layer < 8; layer++)
            {
                var t = layer / 8f;
                var rx = 86 - (layer * 10);
                var ry = 114 - (layer * 13);
                var color = layer % 2 == 0 ? A(Violet, 70) : A(Hex("#4FE0D0"), 65);
                c.FillEllipse(110, 128 + (layer * 2), rx, ry, color);
            }

            c.FillEllipse(110, 150, 34, 66, A(Hex("#0B0614"), 210));
            for (var i = 0; i < 26; i++)
            {
                var angle = (i / 26f) * MathF.Tau;
                var x = 110 + (MathF.Cos(angle) * (72 + (i % 3) * 6));
                var y = 150 + (MathF.Sin(angle) * (104 + (i % 4) * 5));
                c.FillCircle(x, y, 1.6f + (i % 2), A(Lime, 100));
            }

            for (var i = 0; i < 12; i++)
            {
                var angle = (i / 12f) * MathF.Tau;
                c.FillCircle(
                    110 + (MathF.Cos(angle) * 40),
                    92 + (MathF.Sin(angle) * 20),
                    2f,
                    A(Color.White, 100));
            }

            c.FillEllipse(110, 232, 78, 9, A(Color.Black, 80));
        }));
    }

    private Texture2D BuildPlayerBolt()
    {
        return Track(Bake(64, 28, c =>
        {
            c.FillGlow(32, 14, 13, A(Lime, 120));
            c.FillEllipse(32, 14, 24, 7, A(Lime, 90));
            c.FillCapsule(new Vector2(14, 14), new Vector2(46, 14), 5.5f, A(Lime, 230));
            c.FillCapsule(new Vector2(22, 14), new Vector2(44, 14), 2.4f, Color.White);
            c.FillPolygon(new[]
            {
                new Vector2(46, 8), new Vector2(62, 14), new Vector2(46, 20),
            }, A(Lime, 220));
        }));
    }

    private Texture2D BuildBossBolt()
    {
        return Track(Bake(48, 48, c =>
        {
            c.FillGlow(24, 24, 23, A(Blood, 100));
            c.FillCircle(24, 24, 13, A(Blood, 220));
            c.FillCircle(24, 24, 8, A(Hex("#FF8A78"), 240));
            c.FillCircle(24, 24, 3.5f, Color.White);
            for (var i = 0; i < 8; i++)
            {
                var angle = i / 8f * MathF.Tau;
                c.FillCircle(24 + (MathF.Cos(angle) * 16), 24 + (MathF.Sin(angle) * 16), 1.8f, A(Hex("#FFB4A0"), 180));
            }
        }));
    }

    private Texture2D BuildSkull()
    {
        return Track(Bake(96, 96, c =>
        {
            var bone = Bone;
            c.FillCircle(48, 40, 30, Hex("#201822"));
            c.FillEllipse(48, 34, 27, 25, bone);
            c.FillRoundedRect(34, 48, 28, 24, 6, bone);
            c.FillEllipse(36, 34, 9, 11, Hex("#0F0B13"));
            c.FillEllipse(60, 34, 9, 11, Hex("#0F0B13"));
            c.FillCircle(36, 35, 2.6f, Lime);
            c.FillCircle(60, 35, 2.6f, Lime);
            c.FillPolygon(new[]
            {
                new Vector2(45, 45), new Vector2(51, 45), new Vector2(48, 52),
            }, Hex("#0F0B13"));
            for (var i = 0; i < 5; i++)
            {
                c.FillRect(38 + (i * 5), 62, 3, 6, A(Hex("#6E6553"), 255));
            }

            c.Stroke(new Vector2(22, 54), new Vector2(22, 70), 2f, A(bone, 90));
            c.Stroke(new Vector2(74, 54), new Vector2(74, 70), 2f, A(bone, 90));
        }));
    }

    private Texture2D BuildPauseIcon()
    {
        return Track(Bake(56, 56, c =>
        {
            c.FillRoundedRect(15, 12, 8, 32, 3, Color.White);
            c.FillRoundedRect(33, 12, 8, 32, 3, Color.White);
        }, supersample: 2));
    }

    private Texture2D BuildJoystickBase()
    {
        return Track(Bake(260, 260, c =>
        {
            c.FillCircle(130, 130, 122, A(Color.White, 10));
            for (var i = 0; i < 72; i++)
            {
                var angle = i / 72f * MathF.Tau;
                c.FillCircle(130 + (MathF.Cos(angle) * 112), 130 + (MathF.Sin(angle) * 112), 3.2f, A(Color.White, 55));
            }

            for (var i = 0; i < 24; i++)
            {
                var angle = i / 24f * MathF.Tau;
                c.FillCircle(130 + (MathF.Cos(angle) * 74), 130 + (MathF.Sin(angle) * 74), 2f, A(Color.White, 30));
            }
        }, supersample: 2));
    }

    private Texture2D BuildFog()
    {
        return Track(Bake(512, 512, c =>
        {
            var rng = new Random(9911);
            for (var i = 0; i < 34; i++)
            {
                c.FillGlow(rng.Next(40, 472), rng.Next(40, 472), rng.Next(40, 130), A(Color.White, rng.Next(10, 28)));
            }
        }, supersample: 1));
    }

    private Texture2D BuildVignette()
        => Track(Bake(640, 360, c => c.FillVignette(A(Color.Black, 235), 0.32f, 1f), supersample: 1));

    private Texture2D BuildTitleBackdrop()
    {
        return Track(Bake(1280, 720, c =>
        {
            var top = Hex("#080512");
            var bottom = Hex("#243041");
            for (var y = 0; y < 720; y++)
            {
                c.FillRect(0, y, 1280, 1, Mix(top, bottom, MathF.Pow(y / 719f, 1.35f)));
            }

            var rng = new Random(4242);
            for (var i = 0; i < 180; i++)
            {
                var x = rng.Next(20, 1260);
                var y = rng.Next(20, 430);
                var radius = rng.Next(1, 3);
                c.FillCircle(x, y, radius, A(Bone, rng.Next(30, 180)));
            }

            // Moon.
            c.FillGlow(980, 190, 220, A(Hex("#C7B9FF"), 40));
            c.FillCircle(980, 190, 116, A(Hex("#E9DCB8"), 40));
            c.FillCircle(980, 190, 100, Moon);
            c.FillCircle(948, 166, 14, A(Hex("#CDBF9D"), 90));
            c.FillCircle(1010, 220, 20, A(Hex("#CDBF9D"), 70));
            c.FillCircle(1004, 152, 9, A(Hex("#CDBF9D"), 60));
            c.FillCircle(938, 220, 7, A(Hex("#CDBF9D"), 70));

            // Distant hills.
            c.FillPolygon(new[]
            {
                new Vector2(0, 560), new Vector2(180, 470), new Vector2(340, 545),
                new Vector2(540, 440), new Vector2(760, 550), new Vector2(980, 480),
                new Vector2(1180, 550), new Vector2(1280, 500), new Vector2(1280, 720), new Vector2(0, 720),
            }, A(Hex("#111422"), 255));
            c.FillPolygon(new[]
            {
                new Vector2(0, 620), new Vector2(240, 560), new Vector2(480, 630),
                new Vector2(720, 545), new Vector2(960, 635), new Vector2(1180, 580),
                new Vector2(1280, 620), new Vector2(1280, 720), new Vector2(0, 720),
            }, Hex("#0A0A12"));

            // Fog bands.
            for (var i = 0; i < 9; i++)
            {
                c.FillEllipse(rng.Next(-100, 1380), rng.Next(500, 700), rng.Next(180, 420), rng.Next(20, 50), A(Hex("#6B7F8C"), rng.Next(10, 24)));
            }

            // Graveyard silhouette.
            DrawSimpleTombstone(c, 90, 640, 66, 86);
            DrawSimpleTombstone(c, 250, 655, 52, 70);
            DrawSimpleTombstone(c, 1130, 650, 74, 92);
            c.FillCapsule(new Vector2(420, 720), new Vector2(410, 560), 14, Hex("#0A0910"));
            c.FillCapsule(new Vector2(414, 610), new Vector2(360, 530), 7, Hex("#0A0910"));
            c.FillCapsule(new Vector2(416, 604), new Vector2(478, 520), 6, Hex("#0A0910"));
            c.FillCapsule(new Vector2(860, 720), new Vector2(852, 580), 10, Hex("#0A0910"));
            c.FillCapsule(new Vector2(854, 620), new Vector2(806, 556), 5, Hex("#0A0910"));

            c.FillVignette(A(Color.Black, 160), 0.42f, 0.9f);

            // Gate posts to frame the title.
            c.FillRect(70, 120, 12, 600, A(Hex("#191522"), 210));
            c.FillRect(1198, 120, 12, 600, A(Hex("#191522"), 210));
            c.FillCircle(76, 116, 12, A(Hex("#191522"), 210));
            c.FillCircle(1204, 116, 12, A(Hex("#191522"), 210));
        }, supersample: 1));
    }

    private static void DrawSimpleTombstone(Canvas c, float x, float baseY, float width, float height)
    {
        var left = x - (width / 2f);
        var top = baseY - height;
        c.FillRoundedRect(left, top, width, height, width * 0.42f, Hex("#0A0910"));
        c.FillRect(left, baseY - 6, width, 6, Hex("#0A0910"));
        c.Stroke(new Vector2(x - (width * 0.28f), top + (height * 0.36f)), new Vector2(x + (width * 0.28f), top + (height * 0.36f)), 2f, A(Hex("#22202B"), 160));
    }

    private Texture2D Bake(int width, int height, Action<Canvas> paint, int supersample = 3)
    {
        var canvas = new Canvas(width, height, supersample);
        paint(canvas);
        return canvas.CreateTexture(_device);
    }

    private Texture2D Track(Texture2D texture)
    {
        _owned.Add(texture);
        return texture;
    }

    private UiFont TrackFont(UiFont font)
    {
        _fonts.Add(font);
        return font;
    }

    private readonly List<UiFont> _fonts = new();

    private static Stream GetResource(string name)
        => typeof(ArtFactory).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing embedded resource '{name}'.");

    public void Dispose()
    {
        foreach (var font in _fonts)
        {
            font.Dispose();
        }

        foreach (var texture in _owned)
        {
            texture.Dispose();
        }
    }
}
