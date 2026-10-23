using System.Numerics;

namespace Mutants.Core;

/// <summary>
/// A single finger tap can reach the game twice: once as a pressed frame and
/// once as a gesture when the finger lifts. The gesture copy can lag by however
/// long the press lasted, so it is de-duplicated with a wider window; otherwise
/// a slow tap toggles twice and a button looks like it ignored the press.
/// </summary>
public sealed class TapDeduplicator
{
    /// <summary>Within 50 px counts as the same spot.</summary>
    public const float SameSpotRadiusSquared = 2500f;

    public const float PressWindow = 0.08f;

    public const float GestureWindow = 0.5f;

    private Vector2 _lastPosition;
    private float _lastTime = float.NegativeInfinity;

    public Vector2 LastPosition => _lastPosition;

    public float LastTime => _lastTime;

    public bool TryRegister(Vector2 position, float time, bool fromGesture)
    {
        var window = fromGesture ? GestureWindow : PressWindow;
        if (time - _lastTime < window && Vector2.DistanceSquared(position, _lastPosition) < SameSpotRadiusSquared)
        {
            return false;
        }

        _lastPosition = position;
        _lastTime = time;
        return true;
    }
}
