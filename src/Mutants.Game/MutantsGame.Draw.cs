using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mutants.Core;
using Mutants.Game.Rendering;

namespace Mutants.Game;

public sealed partial class MutantsGame
{
    private static readonly Color PanelFill = new((byte)18, (byte)13, (byte)27, (byte)238);
    private static readonly Color PanelEdge = new((byte)76, (byte)62, (byte)102, (byte)255);
    private static readonly Color PanelSoft = new((byte)10, (byte)7, (byte)16, (byte)205);

    private void DrawTitleBackdrop()
    {
        _sprite.Draw(_art.TitleBackdrop, new Rectangle(0, 0, VirtualWidth, VirtualHeight), Color.White);
        _particles.Draw(_sprite, _art.Glow, Vector2.Zero, _time);

        UiPainter.Button(_sprite, _art.Panel, PlayButton, PanelFill, ArtFactory.Lime, selected: true);
        UiPainter.Button(_sprite, _art.Panel, ScoresButton, PanelFill, PanelEdge);
        UiPainter.Button(_sprite, _art.Panel, HelpButton, PanelFill, PanelEdge);
        UiPainter.Button(
            _sprite,
            _art.Panel,
            SoundButton,
            _audio.Muted ? new Color((byte)46, (byte)16, (byte)22, (byte)235) : PanelSoft,
            _audio.Muted ? ArtFactory.Blood : ArtFactory.Lime,
            borderWidth: _audio.Muted ? 3 : 2);
    }

    private void DrawTitleText()
    {
        _sprite.Draw(
            _art.Title,
            new Rectangle(150, 92, 980, 322),
            Color.White);

        _art.Display.DrawCentered(_sprite, "PLAY", PlayButton.Center.X, PlayButton.Y + 20, 34, ArtFactory.Bone, shadow: true);
        _art.Display.DrawCentered(_sprite, "HIGH SCORES", ScoresButton.Center.X, ScoresButton.Y + 17, 24, ArtFactory.Bone);
        _art.Display.DrawCentered(_sprite, "HOW TO SURVIVE", HelpButton.Center.X, HelpButton.Y + 15, 20, ArtFactory.Bone);
        _art.Display.DrawCentered(
            _sprite,
            _audio.Muted ? "SOUND OFF" : "SOUND ON",
            SoundButton.Center.X,
            SoundButton.Y + 15,
            18,
            _audio.Muted ? ArtFactory.Ember : ArtFactory.Bone);

        _art.Body.DrawCentered(
            _sprite,
            "AN ORIGINAL NIGHT-TERROR ADVENTURE",
            VirtualWidth / 2f,
            652,
            18,
            new Color((byte)160, (byte)151, (byte)172),
            alpha: 0.9f);

        _art.Body.Draw(_sprite, "v1.0", new Vector2(36, 684), 16, new Color((byte)120, (byte)113, (byte)134));
        _art.Body.Draw(_sprite, "2026 PAUL F JOHNSON", new Vector2(944, 684), 16, new Color((byte)120, (byte)113, (byte)134));
    }

    private void DrawMenuBackdrop()
    {
        _sprite.Draw(_art.TitleBackdrop, new Rectangle(0, 0, VirtualWidth, VirtualHeight), new Color((byte)150, (byte)150, (byte)160));
        _sprite.Draw(_art.Panel, new Rectangle(0, 0, VirtualWidth, VirtualHeight), new Color((byte)5, (byte)3, (byte)10, (byte)165));
        _particles.Draw(_sprite, _art.Glow, Vector2.Zero, _time);
        _sprite.Draw(_art.Vignette, new Rectangle(0, 0, VirtualWidth, VirtualHeight), Color.White);

        switch (_screen)
        {
            case GameScreen.Difficulty:
                for (var i = 0; i < DifficultyProfile.All.Count; i++)
                {
                    var card = DifficultyCard(i);
                    var selected = i == _selectedDifficulty;
                    UiPainter.Panel(
                        _sprite,
                        _art.Panel,
                        card,
                        selected ? new Color((byte)32, (byte)41, (byte)29, (byte)245) : PanelFill,
                        selected ? ArtFactory.Lime : PanelEdge,
                        borderWidth: selected ? 4 : 2,
                        shadow: selected ? 18 : 9);

                    var profile = DifficultyProfile.All[i];
                    var accent = i switch
                    {
                        0 => ArtFactory.Toxic,
                        1 => ArtFactory.Ember,
                        _ => ArtFactory.Blood,
                    };
                    _sprite.Draw(_art.Panel, new Rectangle(card.X + 26, card.Y + 22, card.Width - 52, 5), accent);
                }

                UiPainter.Button(_sprite, _art.Panel, StartButton, PanelFill, ArtFactory.Lime, selected: true);
                UiPainter.Button(_sprite, _art.Panel, BackButton, PanelFill, PanelEdge);
                break;

            case GameScreen.HowTo:
                UiPainter.Panel(_sprite, _art.Panel, new Rectangle(240, 132, 800, 450), PanelFill, PanelEdge, borderWidth: 3);
                UiPainter.Button(_sprite, _art.Panel, new Rectangle(490, 596, 300, 62), PanelFill, ArtFactory.Lime, selected: true);
                break;

            case GameScreen.HighScores:
                DrawHighScorePanels();
                break;

            case GameScreen.GameOver:
                DrawGameOverPanels();
                break;
        }
    }

    private void DrawDifficultyText()
    {
        _art.Display.DrawCentered(_sprite, "CHOOSE YOUR NIGHT", VirtualWidth / 2f, 58, 50, ArtFactory.Bone, shadow: true);
        _art.Body.DrawCentered(
            _sprite,
            "THE DARK DOES NOT BARGAIN",
            VirtualWidth / 2f,
            124,
            20,
            new Color((byte)169, (byte)158, (byte)184),
            alpha: 0.9f);

        for (var i = 0; i < DifficultyProfile.All.Count; i++)
        {
            var profile = DifficultyProfile.All[i];
            var card = DifficultyCard(i);
            var selected = i == _selectedDifficulty;
            var accent = i switch
            {
                0 => ArtFactory.Toxic,
                1 => ArtFactory.Ember,
                _ => ArtFactory.Blood,
            };

            _art.Body.Draw(
                _sprite,
                $"0{i + 1}",
                new Vector2(card.X + 28, card.Y + 38),
                20,
                accent,
                alpha: 0.9f);

            _art.Display.DrawCentered(_sprite, profile.Name, card.Center.X, card.Y + 78, 34, selected ? ArtFactory.Lime : ArtFactory.Bone, shadow: true);

            var tagline = i switch
            {
                0 => "THE DEAD ARE DROWSY.\nFIVE LIVES, SOFTER BLOWS.",
                1 => "THE HOUR THE GRAVES OPEN.\nTHREE LIVES, FAIR ODDS.",
                _ => "DAWN NEVER COMES.\nTWO LIVES, HUNGRY HORRORS.",
            };
            _art.Body.DrawCentered(_sprite, tagline, card.Center.X, card.Y + 132, 18, new Color((byte)172, (byte)164, (byte)184));

            _sprite.Draw(_art.Panel, new Rectangle(card.X + 28, card.Y + 210, card.Width - 56, 2), new Color((byte)70, (byte)60, (byte)88, (byte)180));

            _art.Body.Draw(_sprite, "LIVES", new Vector2(card.X + 32, card.Y + 228), 17, new Color((byte)145, (byte)137, (byte)158));
            _art.Body.Draw(_sprite, profile.StartingLives.ToString(), new Vector2(card.X + 232, card.Y + 226), 20, ArtFactory.Bone);
            _art.Body.Draw(_sprite, "SCORE", new Vector2(card.X + 32, card.Y + 254), 17, new Color((byte)145, (byte)137, (byte)158));
            _art.Body.Draw(_sprite, $"x{profile.ScoreMultiplier:0.0}", new Vector2(card.X + 232, card.Y + 252), 20, accent);
            _art.Body.Draw(_sprite, "BOSS", new Vector2(card.X + 32, card.Y + 280), 17, new Color((byte)145, (byte)137, (byte)158));
            _art.Body.Draw(_sprite, profile.BossHealth.ToString(), new Vector2(card.X + 232, card.Y + 278), 20, ArtFactory.Bone);
        }

        _art.Display.DrawCentered(_sprite, "ENTER THE NIGHT", StartButton.Center.X, StartButton.Y + 17, 27, ArtFactory.Bone);
        _art.Body.Draw(_sprite, "BACK", new Vector2(BackButton.X + 30, BackButton.Y + 17), 18, new Color((byte)196, (byte)188, (byte)207));
    }

    private void DrawHowToText()
    {
        _art.Display.DrawCentered(_sprite, "HOW TO SURVIVE", VirtualWidth / 2f, 158, 42, ArtFactory.Bone, shadow: true);
        _art.Body.DrawCentered(
            _sprite,
            "THE LANTERN-KEEPER'S LAST INSTRUCTIONS",
            VirtualWidth / 2f,
            216,
            18,
            ArtFactory.Lime,
            alpha: 0.9f);

        var lines = new[]
        {
            "MOVE - DRAG ANYWHERE ON THE LEFT SIDE OF THE SCREEN.",
            "BLAST - HOLD ANYWHERE ON THE RIGHT SIDE OF THE SCREEN.",
            string.Empty,
            "EACH DISTRICT HIDES THREE KEYS. GATHER THEM TO OPEN THE IRON GATE.",
            "BEYOND IT WAITS THE HOLLOW KING. BREAK HIM, THEN STEP INTO THE PORTAL.",
            "VIGOUR CARRIES BETWEEN DISTRICTS. EXTRA LIVES COST YOU A DEATH, NOT A COIN.",
            "THE TOP EIGHT RUNS ARE CARVED INTO THE RECORD AND KEPT BETWEEN VISITS.",
        };

        var y = 260f;
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                y += 10f;
                continue;
            }

            _art.Body.DrawCentered(_sprite, line, VirtualWidth / 2f, y, 18, new Color((byte)204, (byte)196, (byte)216));
            y += 42f;
        }

        _art.Body.DrawCentered(
            _sprite,
            "KEYBOARD: WASD / ARROWS - SPACE TO BLAST - ESC PAUSES - M MUTES",
            VirtualWidth / 2f,
            540,
            16,
            new Color((byte)138, (byte)129, (byte)153));

        _art.Display.DrawCentered(_sprite, "GOT IT", 640, 610, 24, ArtFactory.Bone);
    }

    private void DrawHighScorePanels()
    {
        UiPainter.Panel(_sprite, _art.Panel, new Rectangle(180, 176, 920, 432), PanelFill, PanelEdge, borderWidth: 3);
        _sprite.Draw(_art.Panel, new Rectangle(220, 226, 840, 2), new Color((byte)80, (byte)68, (byte)102, (byte)200));

        for (var row = 0; row < HighScoreTable.Capacity; row++)
        {
            var y = 240 + (row * 43);
            var highlight = row == _highlightRank;
            if (highlight)
            {
                UiPainter.Panel(
                    _sprite,
                    _art.Panel,
                    new Rectangle(200, y - 4, 880, 41),
                    new Color((byte)42, (byte)56, (byte)34, (byte)210),
                    new Color((byte)182, (byte)242, (byte)107, (byte)110),
                    borderWidth: 1,
                    shadow: 0);
            }
            else if (row % 2 == 1)
            {
                _sprite.Draw(_art.Panel, new Rectangle(200, y - 4, 880, 41), UiPainter.Tint(Color.White, 14));
            }
        }

        UiPainter.Button(_sprite, _art.Panel, HighScoresBackButton, PanelFill, ArtFactory.Lime, selected: true);
    }

    private void DrawHighScoreText()
    {
        _art.Display.DrawCentered(_sprite, "HALL OF THE FALLEN", VirtualWidth / 2f, 66, 48, ArtFactory.Bone, shadow: true);
        _art.Body.DrawCentered(
            _sprite,
            "THE EIGHT NIGHTS THAT ENDURED",
            VirtualWidth / 2f,
            128,
            18,
            new Color((byte)169, (byte)158, (byte)184),
            alpha: 0.9f);

        var headerColor = new Color((byte)137, (byte)128, (byte)153);
        _art.Body.Draw(_sprite, "RANK", new Vector2(220, 198), 15, headerColor);
        _art.Body.Draw(_sprite, "INITIALS", new Vector2(300, 198), 15, headerColor);
        _art.Body.Draw(_sprite, "SCORE", new Vector2(562, 198), 15, headerColor);
        _art.Body.Draw(_sprite, "DISTRICT", new Vector2(738, 198), 15, headerColor);
        _art.Body.Draw(_sprite, "NIGHT", new Vector2(920, 198), 15, headerColor);

        if (_scores.Count == 0)
        {
            _art.Display.DrawCentered(
                _sprite,
                "NO NAMES YET",
                VirtualWidth / 2f,
                352,
                34,
                new Color((byte)196, (byte)188, (byte)207),
                shadow: true);
            _art.Body.DrawCentered(
                _sprite,
                "THE RECORD IS YOURS TO TAKE",
                VirtualWidth / 2f,
                402,
                18,
                ArtFactory.Lime,
                alpha: 0.8f);
        }

        for (var row = 0; row < _scores.Entries.Count; row++)
        {
            var entry = _scores.Entries[row];
            var y = 241 + (row * 43);
            var color = row == _highlightRank ? ArtFactory.Lime : ArtFactory.Bone;
            var dim = new Color((byte)158, (byte)149, (byte)172);

            _art.Display.Draw(_sprite, (row + 1).ToString("00"), new Vector2(226, y), 18, row == _highlightRank ? ArtFactory.Lime : dim);
            _art.Display.Draw(_sprite, entry.Initials, new Vector2(300, y - 1), 24, color);
            _art.Display.Draw(_sprite, entry.Score.ToString("000000"), new Vector2(562, y), 20, color);
            _art.Body.Draw(_sprite, RomanLevel(entry.LevelReached), new Vector2(738, y + 2), 19, color);
            _art.Body.Draw(_sprite, entry.Difficulty switch
            {
                Difficulty.Dusk => "DUSK",
                Difficulty.Midnight => "MIDNIGHT",
                _ => "ETERNAL",
            }, new Vector2(920, y + 2), 19, color);
        }

        _art.Body.DrawCentered(
            _sprite,
            _highlightRank >= 0 ? "A NEW NAME IN THE RECORD" : "SCORES SURVIVE EVEN WHEN YOU DO NOT",
            VirtualWidth / 2f,
            596,
            16,
            _highlightRank >= 0 ? ArtFactory.Lime : new Color((byte)137, (byte)128, (byte)153));

        _art.Display.DrawCentered(_sprite, "BACK TO THE DARK", HighScoresBackButton.Center.X, HighScoresBackButton.Y + 16, 24, ArtFactory.Bone);
    }

    private void DrawGameWorld()
    {
        var run = _run;
        if (run is null)
        {
            return;
        }

        var world = run.World;
        _worldRenderer.Draw(_sprite, world, _camera - _shakeOffset, _time);
        _particles.Draw(_sprite, _art.Glow, -_camera + _shakeOffset, _time);

        _sprite.Draw(_art.Vignette, new Rectangle(0, 0, VirtualWidth, VirtualHeight), Color.White);

        if (_flash > 0f)
        {
            _sprite.Draw(
                _art.Panel,
                new Rectangle(0, 0, VirtualWidth, VirtualHeight),
                new Color(_flashColor.R, _flashColor.G, _flashColor.B, (byte)Math.Clamp(_flash * 120f, 0, 255)));
        }

        DrawHudPanels(world);

        if (_screen == GameScreen.Paused)
        {
            DrawPausedPanels();
        }
        else if (_screen == GameScreen.GameOver)
        {
            DrawGameOverPanels();
        }
    }

    private void DrawHudPanels(GameWorld world)
    {
        UiPainter.Panel(_sprite, _art.Panel, new Rectangle(24, 18, 420, 86), PanelSoft, new Color((byte)76, (byte)62, (byte)102, (byte)160), borderWidth: 2, shadow: 0);

        for (var i = 0; i < PlayerState.MaxHealth; i++)
        {
            var filled = i < world.Player.Health;
            var tint = filled ? Color.White : new Color((byte)82, (byte)69, (byte)92, (byte)170);
            _sprite.Draw(_art.Heart, new Rectangle(40 + (i * 34), 30, 30, 28), tint);
        }

        _sprite.Draw(_art.Key, new Rectangle(276, 28, 32, 32), Color.White);
        _sprite.Draw(_art.Coin, new Rectangle(374, 32, 26, 26), Color.White);

        UiPainter.Panel(_sprite, _art.Panel, new Rectangle(934, 24, 228, 58), PanelSoft, new Color((byte)76, (byte)62, (byte)102, (byte)140), borderWidth: 2, shadow: 0);
        UiPainter.Button(_sprite, _art.Panel, PauseButton, PanelSoft, new Color((byte)76, (byte)62, (byte)102, (byte)180), borderWidth: 2);
        _sprite.Draw(_art.PauseIcon, new Rectangle(PauseButton.X + 20, PauseButton.Y + 14, 32, 36), ArtFactory.Bone);

        var half = (int)_difficulty.Difficulty;
        var accent = half switch
        {
            0 => ArtFactory.Toxic,
            1 => ArtFactory.Ember,
            _ => ArtFactory.Blood,
        };
        _sprite.Draw(_art.Panel, new Rectangle(950, 38, 5, 30), accent);

        if (world.Boss.Awake && world.Boss.IsAlive)
        {
            UiPainter.Bar(
                _sprite,
                _art.Panel,
                new Rectangle(390, 112, 500, 34),
                (float)world.Boss.Health / world.Boss.MaxHealth,
                ArtFactory.Blood,
                new Color((byte)15, (byte)8, (byte)14, (byte)230),
                new Color((byte)124, (byte)48, (byte)58, (byte)255));
        }

        if (_toastTimer > 0f)
        {
            UiPainter.Panel(
                _sprite,
                _art.Panel,
                new Rectangle(390, 148, 500, 48),
                new Color((byte)13, (byte)9, (byte)20, (byte)210),
                new Color((byte)0, (byte)0, (byte)0, (byte)0),
                borderWidth: 0,
                shadow: 0);
        }

        if (_screen == GameScreen.Playing && _introTimer <= 0f)
        {
            DrawTouchControls(world);
        }
    }

    private void DrawTouchControls(GameWorld world)
    {
        var joystickCenter = _joystickTouchId >= 0
            ? _joystickOrigin
            : new Vector2(210, 566);
        var alpha = _joystickTouchId >= 0 ? 0.85f : 0.32f;
        _sprite.Draw(
            _art.JoystickBase,
            new Rectangle((int)joystickCenter.X - 105, (int)joystickCenter.Y - 105, 210, 210),
            UiPainter.Tint(Color.White, (int)(255 * alpha)));

        var knob = joystickCenter + (_joystickVector * 70f);
        _sprite.Draw(
            _art.JoystickKnob,
            new Rectangle((int)knob.X - 42, (int)knob.Y - 42, 84, 84),
            UiPainter.Tint(Color.White, (int)(255 * alpha)));

        var attackCenter = new Vector2(1090, 566);
        _sprite.Draw(
            _art.JoystickBase,
            new Rectangle((int)attackCenter.X - 98, (int)attackCenter.Y - 98, 196, 196),
            UiPainter.Tint(ArtFactory.Ember, _attackHeld ? 150 : 70));

        var cooldown = Math.Clamp(world.Player.AttackCooldown / 0.26f, 0f, 1f);
        if (cooldown > 0f)
        {
            _sprite.Draw(
                _art.Glow,
                new Rectangle((int)attackCenter.X - 82, (int)attackCenter.Y - 82, 164, 164),
                UiPainter.Tint(ArtFactory.Ember, (int)(70 * cooldown)));
        }
    }

    private void DrawHudText()
    {
        var run = _run;
        if (run is null)
        {
            return;
        }

        var world = run.World;

        _art.Display.Draw(_sprite, $"SCORE {run.TotalScore:000000}", new Vector2(38, 67), 20, ArtFactory.Bone, shadow: true);
        _art.Display.Draw(_sprite, $"{world.KeysCollected} / {world.RequiredKeys}", new Vector2(314, 35), 18, ArtFactory.Bone);
        _art.Display.Draw(_sprite, $"{(world.GateOpen ? "OPEN" : "GATE")}", new Vector2(406, 36), 14, world.GateOpen ? ArtFactory.Lime : new Color((byte)148, (byte)139, (byte)160));

        _art.Display.DrawCentered(
            _sprite,
            world.Map.Name.ToUpperInvariant(),
            VirtualWidth / 2f,
            28,
            24,
            ArtFactory.Bone,
            shadow: true);
        _art.Body.DrawCentered(
            _sprite,
            $"DISTRICT {RomanLevel(run.LevelNumber)} - {world.Map.Subtitle.ToUpperInvariant()}",
            VirtualWidth / 2f,
            62,
            15,
            new Color((byte)169, (byte)158, (byte)184),
            alpha: 0.9f);

        _art.Display.DrawCentered(_sprite, _difficulty.Name, 1048, 38, 18, ArtFactory.Bone);

        if (world.Boss.Awake && world.Boss.IsAlive)
        {
            _art.Display.DrawCentered(_sprite, "THE HOLLOW KING", 640, 84, 18, new Color((byte)255, (byte)185, (byte)175), shadow: true);
        }

        if (_toastTimer > 0f)
        {
            var alpha = Math.Clamp(_toastTimer / 0.45f, 0f, 1f);
            _art.Display.DrawCentered(_sprite, _toast, 640, 158, 26, _toastColor, alpha, shadow: true);
        }

        if (_introTimer > 0f && _screen == GameScreen.Playing)
        {
            var alpha = Math.Clamp(_introTimer / 0.5f, 0f, 1f);
            _art.Display.DrawCentered(
                _sprite,
                $"DISTRICT {RomanLevel(run.LevelNumber)}",
                VirtualWidth / 2f,
                280,
                22,
                ArtFactory.Lime,
                alpha,
                shadow: true);
            _art.Display.DrawCentered(
                _sprite,
                world.Map.Name.ToUpperInvariant(),
                VirtualWidth / 2f,
                320,
                58,
                ArtFactory.Bone,
                alpha,
                shadow: true);
            _art.Body.DrawCentered(
                _sprite,
                world.Map.Subtitle.ToUpperInvariant(),
                VirtualWidth / 2f,
                394,
                20,
                new Color((byte)190, (byte)181, (byte)204),
                alpha);
        }

        if (_screen == GameScreen.Playing && _introTimer <= 0f)
        {
            _art.Body.Draw(_sprite, "MOVE", new Vector2(182, 682), 15, new Color((byte)210, (byte)202, (byte)220), alpha: 0.8f);
            _art.Body.Draw(_sprite, "BLAST", new Vector2(1062, 682), 15, ArtFactory.Ember, alpha: 0.9f);
            _art.Body.DrawCentered(
                _sprite,
                world.GateOpen ? "THE GATE IS OPEN - FIND THE HOLLOW KING" : "GATHER THREE KEYS TO OPEN THE IRON GATE",
                VirtualWidth / 2f,
                674,
                16,
                new Color((byte)190, (byte)181, (byte)204),
                alpha: 0.75f);
        }
    }

    private void DrawPausedPanels()
    {
        _sprite.Draw(_art.Panel, new Rectangle(0, 0, VirtualWidth, VirtualHeight), new Color((byte)3, (byte)2, (byte)8, (byte)190));
        UiPainter.Panel(_sprite, _art.Panel, new Rectangle(440, 150, 400, 390), PanelFill, new Color((byte)88, (byte)72, (byte)116), borderWidth: 3);
        UiPainter.Button(_sprite, _art.Panel, ResumeButton, PanelFill, ArtFactory.Lime, selected: true);
        UiPainter.Button(_sprite, _art.Panel, RestartButton, PanelFill, PanelEdge);
        UiPainter.Button(_sprite, _art.Panel, QuitButton, PanelFill, PanelEdge);
    }

    private void DrawPausedText()
    {
        _art.Display.DrawCentered(_sprite, "PAUSED", 640, 178, 46, ArtFactory.Bone, shadow: true);
        _art.Body.DrawCentered(_sprite, "THE NIGHT HOLDS ITS BREATH", 640, 238, 16, new Color((byte)169, (byte)158, (byte)184));

        _art.Display.DrawCentered(_sprite, "RESUME", ResumeButton.Center.X, ResumeButton.Y + 15, 24, ArtFactory.Bone);
        _art.Display.DrawCentered(_sprite, "RESTART RUN", RestartButton.Center.X, RestartButton.Y + 15, 22, ArtFactory.Bone);
        _art.Display.DrawCentered(_sprite, "QUIT TO TITLE", QuitButton.Center.X, QuitButton.Y + 15, 20, ArtFactory.Bone);
    }

    private void DrawGameOverPanels()
    {
        _sprite.Draw(_art.Panel, new Rectangle(0, 0, VirtualWidth, VirtualHeight), new Color((byte)3, (byte)2, (byte)8, (byte)205));
        UiPainter.Panel(_sprite, _art.Panel, new Rectangle(300, 74, 680, 610), PanelFill, new Color((byte)88, (byte)72, (byte)116), borderWidth: 3);
        _sprite.Draw(_art.Skull, new Rectangle(604, 96, 72, 72), _victory ? ArtFactory.Lime : ArtFactory.Bone);

        if (_gameOverQualifies && !_scoreSaved)
        {
            UiPainter.Panel(_sprite, _art.Panel, new Rectangle(466, 278, 348, 196), new Color((byte)9, (byte)6, (byte)14, (byte)214), new Color((byte)62, (byte)52, (byte)82), borderWidth: 1, shadow: 0);

            for (var slot = 0; slot < 3; slot++)
            {
                var selected = slot == _selectedSlot;
                UiPainter.Panel(
                    _sprite,
                    _art.Panel,
                    InitialsSlot(slot),
                    selected ? new Color((byte)41, (byte)57, (byte)31, (byte)235) : new Color((byte)20, (byte)15, (byte)29, (byte)235),
                    selected ? ArtFactory.Lime : new Color((byte)81, (byte)68, (byte)102),
                    borderWidth: selected ? 3 : 2,
                    shadow: 0);

                UiPainter.Button(_sprite, _art.Panel, InitialUp(slot), new Color((byte)24, (byte)18, (byte)34, (byte)240), new Color((byte)81, (byte)68, (byte)102), borderWidth: 1);
                UiPainter.Button(_sprite, _art.Panel, InitialDown(slot), new Color((byte)24, (byte)18, (byte)34, (byte)240), new Color((byte)81, (byte)68, (byte)102), borderWidth: 1);
            }

            UiPainter.Button(_sprite, _art.Panel, SaveButton, PanelFill, ArtFactory.Lime, selected: true);
        }
        else
        {
            UiPainter.Button(_sprite, _art.Panel, GameOverScoresButton, PanelFill, ArtFactory.Lime, selected: true);
            UiPainter.Button(_sprite, _art.Panel, GameOverTitleButton, PanelFill, PanelEdge);
        }
    }

    private void DrawGameOverText()
    {
        _art.Display.DrawCentered(
            _sprite,
            _victory ? "DAWN BREAKS" : "THE NIGHT CLAIMS YOU",
            VirtualWidth / 2f,
            104,
            48,
            _victory ? ArtFactory.Lime : ArtFactory.Bone,
            shadow: true);

        _art.Body.DrawCentered(
            _sprite,
            _victory ? "THE HAUNTING HOUR ENDS. FOR NOW." : "THE LANTERN GOES OUT.",
            VirtualWidth / 2f,
            168,
            18,
            new Color((byte)181, (byte)172, (byte)198));

        _art.Display.DrawCentered(_sprite, $"SCORE {_pendingScore:000000}", VirtualWidth / 2f, 202, 32, ArtFactory.Bone);
        _art.Body.DrawCentered(
            _sprite,
            $"DISTRICT {RomanLevel(Math.Clamp(_run?.LevelNumber ?? 1, 1, LevelCatalog.Count))} - {_difficulty.Name}",
            VirtualWidth / 2f,
            246,
            17,
            new Color((byte)169, (byte)158, (byte)184));

        if (_gameOverQualifies && !_scoreSaved)
        {
            _art.Display.DrawCentered(_sprite, "CARVE YOUR INITIALS", VirtualWidth / 2f, 294, 18, ArtFactory.Lime);
            _art.Body.DrawCentered(_sprite, "TAP THE ARROWS TO CHANGE - THEN SAVE", VirtualWidth / 2f, 448, 15, new Color((byte)158, (byte)149, (byte)172));

            for (var slot = 0; slot < 3; slot++)
            {
                var rect = InitialsSlot(slot);
                _art.Display.DrawCentered(_sprite, _initials[slot].ToString(), rect.Center.X, rect.Y + 20, 46, slot == _selectedSlot ? ArtFactory.Lime : ArtFactory.Bone, shadow: true);
                _art.Body.DrawCentered(_sprite, "^", InitialUp(slot).Center.X, InitialUp(slot).Y + 9, 18, new Color((byte)214, (byte)205, (byte)226));
                _art.Body.DrawCentered(_sprite, "v", InitialDown(slot).Center.X, InitialDown(slot).Y + 9, 18, new Color((byte)214, (byte)205, (byte)226));
            }

            if (!string.IsNullOrEmpty(_saveError))
            {
                _art.Body.DrawCentered(_sprite, _saveError, VirtualWidth / 2f, 480, 15, ArtFactory.Blood);
            }

            _art.Display.DrawCentered(_sprite, "SAVE THE RECORD", SaveButton.Center.X, SaveButton.Y + 16, 23, ArtFactory.Bone);
        }
        else
        {
            _art.Body.DrawCentered(
                _sprite,
                _gameOverQualifies ? "THE RECORD REMEMBERS YOU" : "THE RECORD KEEPS ITS DISTANCE",
                VirtualWidth / 2f,
                286,
                18,
                _gameOverQualifies ? ArtFactory.Lime : new Color((byte)158, (byte)149, (byte)172));

            _art.Display.DrawCentered(_sprite, "HIGH SCORES", GameOverScoresButton.Center.X, GameOverScoresButton.Y + 15, 22, ArtFactory.Bone);
            _art.Display.DrawCentered(_sprite, "TITLE SCREEN", GameOverTitleButton.Center.X, GameOverTitleButton.Y + 15, 20, ArtFactory.Bone);
        }
    }

    private static string RomanLevel(int level)
        => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => level.ToString(),
        };
}
