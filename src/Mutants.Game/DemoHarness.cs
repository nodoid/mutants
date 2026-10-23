using Microsoft.Xna.Framework;
using Mutants.Core;
using NVector2 = System.Numerics.Vector2;

namespace Mutants.Game;

/// <summary>
/// Temporary capture harness: lets the store-screenshot scripts open a given
/// screen and feeds scripted input so gameplay footage has movement and blasts.
/// </summary>
public sealed partial class MutantsGame
{
    private readonly bool _demoAutoPlay = Environment.GetEnvironmentVariable("MUTANTS_DEMO") == "1";
    private readonly string? _demoScreen = Environment.GetEnvironmentVariable("MUTANTS_SCREEN");
    private float _demoTime;
    private bool _demoStarted;

    private void DemoStart()
    {
        if (_demoStarted)
        {
            return;
        }

        _demoStarted = true;

        switch (_demoScreen?.ToLowerInvariant())
        {
            case "difficulty":
                ChangeScreen(GameScreen.Difficulty);
                return;
            case "howto":
                ChangeScreen(GameScreen.HowTo);
                return;
            case "scores":
                ChangeScreen(GameScreen.HighScores);
                return;
            case "gameover":
                StartRun(Difficulty.Dusk);
                _pendingScore = 12480;
                _gameOverQualifies = true;
                ChangeScreen(GameScreen.GameOver);
                return;
        }

        if (_demoAutoPlay)
        {
            StartRun(Difficulty.Dusk);
        }
    }

    private void DemoUpdate(float dt)
    {
        if (!_demoAutoPlay || _screen != GameScreen.Playing || _run is null)
        {
            return;
        }

        _demoTime += dt;

        // Walk a lazy circle so the camera scrolls and the sprite faces around.
        var angle = _demoTime * 0.7f;
        _joystickVector = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 0.85f;

        // Burst-fire so the HUD, particles and enemies all show up.
        _attackHeld = _demoTime % 1.8f < 0.9f;

        // Skip the district title card so footage is actual play.
        _introTimer = 0f;
    }
}
