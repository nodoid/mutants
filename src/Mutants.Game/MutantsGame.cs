using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Mutants.Core;
using Mutants.Game.Rendering;
using NVector2 = System.Numerics.Vector2;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Mutants.Game;

public enum GameScreen
{
    Title,
    Difficulty,
    HowTo,
    Playing,
    Paused,
    GameOver,
    HighScores,
}

/// <summary>Main MonoGame application: owns presentation and the live run.</summary>
public sealed partial class MutantsGame : Microsoft.Xna.Framework.Game
{
    public const int VirtualWidth = 1280;
    public const int VirtualHeight = 720;

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private static readonly Rectangle PlayButton = new(490, 392, 300, 76);
    private static readonly Rectangle ScoresButton = new(490, 486, 300, 64);
    private static readonly Rectangle HelpButton = new(490, 568, 300, 56);
    private static readonly Rectangle BackButton = new(32, 26, 156, 56);
    private static readonly Rectangle StartButton = new(490, 588, 300, 74);
    private static readonly Rectangle PauseButton = new(1180, 28, 72, 64);
    private static readonly Rectangle ResumeButton = new(490, 296, 300, 62);
    private static readonly Rectangle RestartButton = new(490, 378, 300, 62);
    private static readonly Rectangle QuitButton = new(490, 460, 300, 62);
    private static readonly Rectangle GameOverScoresButton = new(490, 540, 300, 58);
    private static readonly Rectangle GameOverTitleButton = new(490, 614, 300, 58);
    private static readonly Rectangle SaveButton = new(490, 540, 300, 64);
    private static readonly Rectangle HighScoresBackButton = new(490, 626, 300, 60);

    private readonly GraphicsDeviceManager _graphics;
    private readonly FileHighScoreStore _store = new();
    private readonly ParticleSystem _particles = new();
    private readonly Random _random = new(1979);
    private readonly List<Vector2> _taps = new();
    private readonly char[] _initials = { 'A', 'A', 'A' };

    private SpriteBatch _sprite = null!;
    private RenderTarget2D _scene = null!;
    private ArtFactory _art = null!;
    private WorldRenderer _worldRenderer = null!;
    private HighScoreTable _scores = new();
    private RunState? _run;
    private GameWorld? _seenWorld;
    private DifficultyProfile _difficulty = DifficultyProfile.For(Difficulty.Midnight);
    private GameScreen _screen = GameScreen.Title;
    private Rectangle _destination;
    private Vector2 _camera;
    private Vector2 _shakeOffset;
    private KeyboardState _keyboard;
    private KeyboardState _previousKeyboard;
    private float _time;
    private float _introTimer;
    private float _toastTimer;
    private string _toast = string.Empty;
    private Color _toastColor = ArtFactory.Bone;
    private float _shake;
    private float _flash;
    private Color _flashColor = Color.White;
    private int _joystickTouchId = -1;
    private int _attackTouchId = -1;
    private Vector2 _joystickOrigin;
    private Vector2 _joystickVector;
    private bool _attackHeld;
    private bool _pauseTap;
    private Vector2 _lastTapPosition;
    private float _lastTapTime = -10f;
    private int _selectedDifficulty = 1;
    private int _selectedSlot;
    private int _pendingScore;
    private int _highlightRank = -1;
    private bool _gameOverQualifies;
    private bool _scoreSaved;
    private bool _victory;
    private string _saveError = string.Empty;

    public MutantsGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = VirtualWidth,
            PreferredBackBufferHeight = VirtualHeight,
            SynchronizeWithVerticalRetrace = true,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = false;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        InactiveSleepTime = TimeSpan.Zero;
    }

    public GameScreen Screen => _screen;

    public bool HasActiveRun => _run is { IsFinished: false };

    protected override void Initialize()
    {
        Window.AllowUserResizing = false;
        TouchPanel.EnableMouseTouchPoint = true;
        TouchPanel.EnabledGestures = GestureType.Tap;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _sprite = new SpriteBatch(GraphicsDevice);
        _art = new ArtFactory(GraphicsDevice);
        _worldRenderer = new WorldRenderer(_art);
        _scene = new RenderTarget2D(
            GraphicsDevice,
            VirtualWidth,
            VirtualHeight,
            mipMap: false,
            SurfaceFormat.Color,
            DepthFormat.None);

        _scores = _store.Load();
        Debug.WriteLine($"Loaded {_scores.Count} high scores from {_store.FilePath}");
        _camera = new Vector2(0, 0);
        UpdateViewport();
    }

    protected override void UnloadContent()
    {
        _scene?.Dispose();
        _art?.Dispose();
        _sprite?.Dispose();
        base.UnloadContent();
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        if (_screen == GameScreen.Playing)
        {
            ChangeScreen(GameScreen.Paused);
        }

        base.OnDeactivated(sender, args);
    }

    protected override void Update(GameTime gameTime)
    {
        var dt = MathF.Min(0.05f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        _time += dt;
        _keyboard = Keyboard.GetState();
        UpdateViewport();
        ReadInput();

        _particles.Update(dt);
        _shake = MathF.Max(0f, _shake - (dt * 18f));
        _flash = MathF.Max(0f, _flash - (dt * 2.4f));
        _toastTimer = MathF.Max(0f, _toastTimer - dt);

        switch (_screen)
        {
            case GameScreen.Title:
                UpdateTitle(dt);
                break;
            case GameScreen.Difficulty:
                UpdateDifficulty(dt);
                break;
            case GameScreen.HowTo:
                UpdateHowTo(dt);
                break;
            case GameScreen.Playing:
                UpdatePlaying(dt);
                break;
            case GameScreen.Paused:
                UpdatePaused();
                break;
            case GameScreen.GameOver:
                UpdateGameOver(dt);
                break;
            case GameScreen.HighScores:
                UpdateHighScores(dt);
                break;
        }

        _shakeOffset = _shake > 0.1f
            ? new Vector2(
                ((float)_random.NextDouble() * 2f - 1f) * _shake,
                ((float)_random.NextDouble() * 2f - 1f) * _shake)
            : Vector2.Zero;

        _previousKeyboard = _keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_scene);
        GraphicsDevice.Clear(ArtFactory.Ink);

        _sprite.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        switch (_screen)
        {
            case GameScreen.Title:
                DrawTitleBackdrop();
                break;
            case GameScreen.Difficulty:
                DrawMenuBackdrop();
                break;
            case GameScreen.HowTo:
                DrawMenuBackdrop();
                break;
            case GameScreen.Playing:
            case GameScreen.Paused:
            case GameScreen.GameOver:
                DrawGameWorld();
                break;
            case GameScreen.HighScores:
                DrawMenuBackdrop();
                break;
        }

        _sprite.End();

        _sprite.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        switch (_screen)
        {
            case GameScreen.Title:
                DrawTitleText();
                break;
            case GameScreen.Difficulty:
                DrawDifficultyText();
                break;
            case GameScreen.HowTo:
                DrawHowToText();
                break;
            case GameScreen.Playing:
                DrawHudText();
                break;
            case GameScreen.Paused:
                DrawHudText();
                DrawPausedText();
                break;
            case GameScreen.GameOver:
                DrawGameOverText();
                break;
            case GameScreen.HighScores:
                DrawHighScoreText();
                break;
        }

        _sprite.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(ArtFactory.Ink);

        _sprite.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        _sprite.Draw(_scene, _destination, Color.White);
        _sprite.End();

        base.Draw(gameTime);
    }

    private void UpdateTitle(float dt)
    {
        _particles.Ambient(LevelTheme.Graveyard, new RectangleF(0, 0, VirtualWidth, VirtualHeight), dt);

        if (Tapped(PlayButton) || KeyPressed(Keys.Enter) || KeyPressed(Keys.Space))
        {
            ChangeScreen(GameScreen.Difficulty);
        }
        else if (Tapped(ScoresButton))
        {
            _highlightRank = -1;
            ChangeScreen(GameScreen.HighScores);
        }
        else if (Tapped(HelpButton))
        {
            ChangeScreen(GameScreen.HowTo);
        }
    }

    private void UpdateDifficulty(float dt)
    {
        _particles.Ambient(LevelTheme.Chapel, new RectangleF(0, 0, VirtualWidth, VirtualHeight), dt);

        for (var i = 0; i < DifficultyProfile.All.Count; i++)
        {
            if (Tapped(DifficultyCard(i)))
            {
                _selectedDifficulty = i;
            }
        }

        if (KeyPressed(Keys.Left) || KeyPressed(Keys.A))
        {
            _selectedDifficulty = Math.Max(0, _selectedDifficulty - 1);
        }

        if (KeyPressed(Keys.Right) || KeyPressed(Keys.D))
        {
            _selectedDifficulty = Math.Min(DifficultyProfile.All.Count - 1, _selectedDifficulty + 1);
        }

        if (Tapped(StartButton) || KeyPressed(Keys.Enter) || KeyPressed(Keys.Space))
        {
            StartRun(DifficultyProfile.All[_selectedDifficulty].Difficulty);
        }
        else if (Tapped(BackButton) || KeyPressed(Keys.Escape))
        {
            ChangeScreen(GameScreen.Title);
        }
    }

    private void UpdateHowTo(float dt)
    {
        _particles.Ambient(LevelTheme.Graveyard, new RectangleF(0, 0, VirtualWidth, VirtualHeight), dt);

        if (Tapped(new Rectangle(490, 596, 300, 62)) || KeyPressed(Keys.Escape) || KeyPressed(Keys.Enter))
        {
            ChangeScreen(GameScreen.Title);
        }
    }

    private void UpdatePlaying(float dt)
    {
        if (Tapped(PauseButton) || _pauseTap || KeyPressed(Keys.Escape) || KeyPressed(Keys.P))
        {
            ChangeScreen(GameScreen.Paused);
            return;
        }

        var run = _run;
        if (run is null || run.IsFinished)
        {
            return;
        }

        if (_introTimer > 0f)
        {
            _introTimer = MathF.Max(0f, _introTimer - dt);
            UpdateCamera(dt, run.World);
            _particles.Ambient(run.World.Map.Theme, CameraView(run.World), dt);
            return;
        }

        var oldWorld = run.World;
        var input = new GameInput(
            new NVector2(_joystickVector.X, _joystickVector.Y),
            _attackHeld || IsAttackKeyDown());

        run.Update(dt, input);
        _particles.Ambient(run.World.Map.Theme, CameraView(run.World), dt);

        if (run.IsFinished)
        {
            EnterGameOver();
            return;
        }

        if (!ReferenceEquals(_seenWorld, run.World))
        {
            _seenWorld = run.World;
            _introTimer = 1.8f;
            _camera = ClampCamera(run.World);
            _particles.Clear();
            ShowToast(
                oldWorld.Status == WorldStatus.Cleared ? "THE PATH DESCENDS" : "THE LANTERN BURNS AGAIN",
                ArtFactory.Bone,
                2.4f);
        }
        else
        {
            ConsumeWorldEvents(run.World);
        }

        UpdateCamera(dt, run.World);
    }

    private void UpdatePaused()
    {
        if (Tapped(ResumeButton) || KeyPressed(Keys.Escape) || KeyPressed(Keys.P))
        {
            ChangeScreen(GameScreen.Playing);
            return;
        }

        if (Tapped(RestartButton))
        {
            StartRun(_difficulty.Difficulty);
            return;
        }

        if (Tapped(QuitButton))
        {
            _run = null;
            _seenWorld = null;
            _particles.Clear();
            ChangeScreen(GameScreen.Title);
        }
    }

    private void UpdateGameOver(float dt)
    {
        _particles.Ambient(LevelTheme.Clocktower, new RectangleF(0, 0, VirtualWidth, VirtualHeight), dt);

        if (_gameOverQualifies && !_scoreSaved)
        {
            UpdateInitialEntry();

            var saveTapped = Tapped(SaveButton);
            if (KeyPressed(Keys.Enter) || saveTapped)
            {
                SavePendingScore();
                return;
            }

            if (KeyPressed(Keys.Escape))
            {
                ChangeScreen(GameScreen.Title);
            }

            return;
        }

        if (Tapped(GameOverScoresButton) || KeyPressed(Keys.Enter))
        {
            ChangeScreen(GameScreen.HighScores);
        }
        else if (Tapped(GameOverTitleButton) || KeyPressed(Keys.Escape))
        {
            ChangeScreen(GameScreen.Title);
        }
    }

    private void UpdateInitialEntry()
    {
        for (var slot = 0; slot < 3; slot++)
        {
            var slotRect = InitialsSlot(slot);
            if (Tapped(slotRect))
            {
                _selectedSlot = slot;
            }

            if (Tapped(InitialUp(slot)))
            {
                _selectedSlot = slot;
                ChangeInitial(slot, 1);
            }

            if (Tapped(InitialDown(slot)))
            {
                _selectedSlot = slot;
                ChangeInitial(slot, -1);
            }
        }

        if (KeyPressed(Keys.Left))
        {
            _selectedSlot = (_selectedSlot + 2) % 3;
        }

        if (KeyPressed(Keys.Right))
        {
            _selectedSlot = (_selectedSlot + 1) % 3;
        }

        if (KeyPressed(Keys.Up))
        {
            ChangeInitial(_selectedSlot, 1);
        }

        if (KeyPressed(Keys.Down))
        {
            ChangeInitial(_selectedSlot, -1);
        }
    }

    private void UpdateHighScores(float dt)
    {
        _particles.Ambient(LevelTheme.Graveyard, new RectangleF(0, 0, VirtualWidth, VirtualHeight), dt);

        if (Tapped(HighScoresBackButton) || KeyPressed(Keys.Escape) || KeyPressed(Keys.Enter))
        {
            ChangeScreen(GameScreen.Title);
        }
    }

    private void ReadInput()
    {
        _taps.Clear();
        _pauseTap = false;

        var touchState = TouchPanel.GetState();
        foreach (var touch in touchState)
        {
            var position = ScreenToVirtual(new Vector2(touch.Position.X, touch.Position.Y));

            if (touch.State == TouchLocationState.Pressed)
            {
                RegisterTap(position);

                if (_screen == GameScreen.Playing && !Contains(PauseButton, position))
                {
                    if (position.X < VirtualWidth * 0.48f)
                    {
                        _joystickTouchId = touch.Id;
                        _joystickOrigin = position;
                        _joystickVector = Vector2.Zero;
                    }
                    else
                    {
                        _attackTouchId = touch.Id;
                        _attackHeld = true;
                    }
                }
            }
            else if (touch.State == TouchLocationState.Moved)
            {
                if (touch.Id == _joystickTouchId)
                {
                    var delta = position - _joystickOrigin;
                    var length = delta.Length();
                    if (length > 96f)
                    {
                        delta = Vector2.Normalize(delta) * 96f;
                    }

                    _joystickVector = length < 14f ? Vector2.Zero : delta / 96f;
                    if (_joystickVector.Length() < 0.16f)
                    {
                        _joystickVector = Vector2.Zero;
                    }
                }
            }
        }

        if (_joystickTouchId >= 0 && !HasActiveTouch(touchState, _joystickTouchId))
        {
            _joystickTouchId = -1;
            _joystickVector = Vector2.Zero;
        }

        if (_attackTouchId >= 0 && !HasActiveTouch(touchState, _attackTouchId))
        {
            _attackTouchId = -1;
            _attackHeld = false;
        }

        // Gesture recognition catches taps that begin and end between frames.
        while (TouchPanel.IsGestureAvailable)
        {
            var gesture = TouchPanel.ReadGesture();
            if (gesture.GestureType == GestureType.Tap)
            {
                RegisterTap(ScreenToVirtual(new Vector2(gesture.Position.X, gesture.Position.Y)));
            }
        }

        if (_screen != GameScreen.Playing)
        {
            _joystickTouchId = -1;
            _attackTouchId = -1;
            _joystickVector = Vector2.Zero;
            _attackHeld = false;
        }
    }

    private void RegisterTap(Vector2 position)
    {
        if (_time - _lastTapTime < 0.08f && Vector2.DistanceSquared(position, _lastTapPosition) < 900f)
        {
            return;
        }

        _lastTapPosition = position;
        _lastTapTime = _time;
        _taps.Add(position);

        if (_screen == GameScreen.Playing && Contains(PauseButton, position))
        {
            _pauseTap = true;
        }
    }

    private static bool HasActiveTouch(TouchCollection touches, int id)
    {
        foreach (var touch in touches)
        {
            if (touch.Id == id && touch.State != TouchLocationState.Released)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAttackKeyDown()
        => _keyboard.IsKeyDown(Keys.Space) || _keyboard.IsKeyDown(Keys.Z) || _keyboard.IsKeyDown(Keys.Enter);

    private void StartRun(Difficulty difficulty)
    {
        _difficulty = DifficultyProfile.For(difficulty);
        _selectedDifficulty = (int)difficulty;
        _run = new RunState(_difficulty);
        _seenWorld = _run.World;
        _camera = ClampCamera(_run.World);
        _introTimer = 2.4f;
        _particles.Clear();
        _shake = 0f;
        _flash = 0f;
        _toastTimer = 0f;
        _highlightRank = -1;
        _saveError = string.Empty;
        ChangeScreen(GameScreen.Playing);
    }

    private void EnterGameOver()
    {
        if (_run is null)
        {
            return;
        }

        _victory = _run.Outcome == RunOutcome.Victory;
        _pendingScore = _run.TotalScore;
        _gameOverQualifies = _scores.Qualifies(_pendingScore);
        _scoreSaved = false;
        _highlightRank = -1;
        _selectedSlot = 0;
        _initials[0] = 'A';
        _initials[1] = 'A';
        _initials[2] = 'A';
        _saveError = string.Empty;
        _flash = 0.8f;
        _flashColor = _victory ? ArtFactory.Lime : ArtFactory.Blood;
        ChangeScreen(GameScreen.GameOver);
    }

    private void SavePendingScore()
    {
        if (_run is null || _scoreSaved)
        {
            return;
        }

        var entry = ScoreEntry.Create(
            new string(_initials),
            _pendingScore,
            _difficulty.Difficulty,
            Math.Clamp(_run.LevelNumber, 1, LevelCatalog.Count),
            DateTimeOffset.UtcNow);

        if (_scores.Add(entry, out var rank))
        {
            _highlightRank = rank;
            _scoreSaved = true;

            try
            {
                _store.Save(_scores);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _saveError = "COULD NOT WRITE THE RECORD";
                Debug.WriteLine(exception);
            }

            ChangeScreen(GameScreen.HighScores);
        }
    }

    private void ChangeScreen(GameScreen screen)
    {
        _screen = screen;
        _joystickTouchId = -1;
        _attackTouchId = -1;
        _joystickVector = Vector2.Zero;
        _attackHeld = false;
        _pauseTap = false;
    }

    private void UpdateCamera(float dt, GameWorld world)
    {
        var target = new Vector2(
            world.Player.Position.X - (VirtualWidth / 2f),
            world.Player.Position.Y - (VirtualHeight / 2f));

        target = new Vector2(
            Math.Clamp(target.X, 0f, Math.Max(0, world.Map.Width - VirtualWidth)),
            Math.Clamp(target.Y, 0f, Math.Max(0, world.Map.Height - VirtualHeight)));

        var blend = 1f - MathF.Exp(-dt * 7.5f);
        _camera = Vector2.Lerp(_camera, target, blend);
    }

    private Vector2 ClampCamera(GameWorld world)
        => new(
            Math.Clamp(world.Player.Position.X - (VirtualWidth / 2f), 0f, Math.Max(0, world.Map.Width - VirtualWidth)),
            Math.Clamp(world.Player.Position.Y - (VirtualHeight / 2f), 0f, Math.Max(0, world.Map.Height - VirtualHeight)));

    private RectangleF CameraView(GameWorld world)
        => new(_camera.X, _camera.Y, VirtualWidth, VirtualHeight);

    private void ConsumeWorldEvents(GameWorld world)
    {
        foreach (var evt in world.Events)
        {
            var position = new NVector2(evt.Position.X, evt.Position.Y);

            switch (evt.Type)
            {
                case WorldEventType.Coin:
                    _particles.Burst(position, ArtFactory.Steel, 6, 90f, 0.5f, 14f);
                    break;

                case WorldEventType.Key:
                    _particles.Burst(position, ArtFactory.Gold, 18, 160f, 0.9f, 22f);
                    ShowToast($"KEY {evt.Value} OF 3", ArtFactory.Gold, 2f);
                    break;

                case WorldEventType.Heart:
                    _particles.Burst(position, ArtFactory.Blood, 14, 140f, 0.8f, 20f);
                    ShowToast("VIGOUR RESTORED", ArtFactory.Blood, 1.8f);
                    break;

                case WorldEventType.EnemyHit:
                    _particles.Burst(position, ArtFactory.Lime, 5, 110f, 0.35f, 12f);
                    break;

                case WorldEventType.EnemyKilled:
                    _particles.Burst(position, ArtFactory.Toxic, 20, 220f, 0.9f, 24f);
                    _particles.Burst(position, ArtFactory.Violet, 8, 150f, 0.8f, 18f);
                    break;

                case WorldEventType.PlayerHurt:
                    _particles.Burst(position, ArtFactory.Blood, 16, 200f, 0.8f, 22f);
                    _shake = MathF.Min(16f, _shake + 10f);
                    _flash = 0.45f;
                    _flashColor = ArtFactory.Blood;
                    break;

                case WorldEventType.GateOpened:
                    _particles.Burst(world.Map.GatePosition, ArtFactory.Lime, 32, 260f, 1.2f, 26f);
                    ShowToast("THE GATE IS OPEN", ArtFactory.Lime, 2.8f);
                    _shake = 8f;
                    _flash = 0.35f;
                    _flashColor = ArtFactory.Lime;
                    break;

                case WorldEventType.BossAwakened:
                    _shake = 14f;
                    ShowToast("THE HOLLOW KING AWAKENS", ArtFactory.Blood, 3f);
                    break;

                case WorldEventType.BossDefeated:
                    _particles.Burst(position, ArtFactory.Blood, 48, 320f, 1.4f, 34f);
                    _particles.Burst(position, ArtFactory.Ember, 30, 260f, 1.1f, 26f);
                    ShowToast("THE GATEKEEPER FALLS", ArtFactory.Ember, 3f);
                    _shake = 20f;
                    _flash = 0.8f;
                    _flashColor = ArtFactory.Ember;
                    break;

                case WorldEventType.ShotFired:
                    _particles.Trail(position, new NVector2(world.Player.Facing.X, world.Player.Facing.Y), ArtFactory.Lime, 2);
                    break;
            }
        }
    }

    private void ShowToast(string text, Color color, float seconds)
    {
        _toast = text;
        _toastColor = color;
        _toastTimer = seconds;
    }

    private void ChangeInitial(int slot, int delta)
    {
        var index = Alphabet.IndexOf(_initials[slot]);
        index = (index + delta + Alphabet.Length) % Alphabet.Length;
        _initials[slot] = Alphabet[index];
    }

    private void UpdateViewport()
    {
        var backBufferWidth = Math.Max(1, GraphicsDevice.PresentationParameters.BackBufferWidth);
        var backBufferHeight = Math.Max(1, GraphicsDevice.PresentationParameters.BackBufferHeight);
        _destination = Letterbox(backBufferWidth, backBufferHeight);
    }

    private static Rectangle Letterbox(int surfaceWidth, int surfaceHeight)
    {
        var scale = MathF.Min(surfaceWidth / (float)VirtualWidth, surfaceHeight / (float)VirtualHeight);
        var width = Math.Max(1, (int)MathF.Round(VirtualWidth * scale));
        var height = Math.Max(1, (int)MathF.Round(VirtualHeight * scale));
        return new Rectangle((surfaceWidth - width) / 2, (surfaceHeight - height) / 2, width, height);
    }

    /// <summary>
    /// Touch panels report in the game's requested display space (typically
    /// stretched to 1280x720) while the game is letterboxed inside the actual
    /// window. Normalise through the window's real aspect before converting.
    /// </summary>
    private Vector2 ScreenToVirtual(Vector2 screenPosition)
    {
        var backBufferWidth = Math.Max(1, GraphicsDevice.PresentationParameters.BackBufferWidth);
        var backBufferHeight = Math.Max(1, GraphicsDevice.PresentationParameters.BackBufferHeight);
        var displayWidth = TouchPanel.DisplayWidth > 0 ? TouchPanel.DisplayWidth : VirtualWidth;
        var displayHeight = TouchPanel.DisplayHeight > 0 ? TouchPanel.DisplayHeight : VirtualHeight;

        var normalizedX = screenPosition.X / displayWidth;
        var normalizedY = screenPosition.Y / displayHeight;

        var left = _destination.X / (float)backBufferWidth;
        var top = _destination.Y / (float)backBufferHeight;
        var width = _destination.Width / (float)backBufferWidth;
        var height = _destination.Height / (float)backBufferHeight;

        if (width <= 0f || height <= 0f)
        {
            return Vector2.Zero;
        }

        return new Vector2(
            ((normalizedX - left) / width) * VirtualWidth,
            ((normalizedY - top) / height) * VirtualHeight);
    }

    private bool Tapped(Rectangle rectangle)
        => _taps.Any(tap => Contains(rectangle, tap));

    private static bool Contains(Rectangle rectangle, Vector2 point)
        => point.X >= rectangle.Left && point.X <= rectangle.Right && point.Y >= rectangle.Top && point.Y <= rectangle.Bottom;

    private bool KeyPressed(Keys key)
        => _keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private static Rectangle DifficultyCard(int index)
        => new(64 + (index * 392), 196, 368, 336);

    private static Rectangle InitialsSlot(int slot)
        => new(524 + (slot * 84), 338, 68, 92);

    private static Rectangle InitialUp(int slot)
        => new(524 + (slot * 84), 286, 68, 42);

    private static Rectangle InitialDown(int slot)
        => new(524 + (slot * 84), 438, 68, 42);

    // Drawing state accessors used by the partial draw file.
    private HighScoreTable Scores => _scores;
}
