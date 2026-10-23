using System.Numerics;
using Mutants.Core;

namespace Mutants.Core.Tests;

public sealed class TapDeduplicatorTests
{
    private static readonly Vector2 Spot = new(1147, 51);

    [Fact]
    public void FirstTapIsAlwaysAccepted()
    {
        var filter = new TapDeduplicator();

        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));
    }

    [Fact]
    public void GestureCopyOfASlowPressIsIgnored()
    {
        var filter = new TapDeduplicator();
        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));

        // The finger lifted 300 ms later, so the gesture arrives long after the press.
        Assert.False(filter.TryRegister(Spot, 10.30f, fromGesture: true));
    }

    [Fact]
    public void FastTapThatNeverProducedAPressFrameIsAccepted()
    {
        var filter = new TapDeduplicator();

        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: true));
    }

    [Fact]
    public void PressThenGestureInTheSameFrameIsIgnored()
    {
        var filter = new TapDeduplicator();
        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));

        Assert.False(filter.TryRegister(Spot, 10.016f, fromGesture: true));
    }

    [Fact]
    public void TwoDeliberatePressesToggleTwice()
    {
        var filter = new TapDeduplicator();

        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));
        Assert.True(filter.TryRegister(Spot, 10.40f, fromGesture: false));
    }

    [Fact]
    public void DuplicatePressFramesAreCollapsed()
    {
        var filter = new TapDeduplicator();
        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));

        Assert.False(filter.TryRegister(Spot, 10.03f, fromGesture: false));
    }

    [Fact]
    public void TapElsewhereIsAcceptedImmediately()
    {
        var filter = new TapDeduplicator();
        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));

        Assert.True(filter.TryRegister(new Vector2(200, 600), 10.02f, fromGesture: true));
    }

    [Fact]
    public void SameSpotLaterIsAcceptedAgain()
    {
        var filter = new TapDeduplicator();
        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));

        Assert.True(filter.TryRegister(Spot, 11f, fromGesture: true));
    }

    [Fact]
    public void SecondTapOfADoubleTapIsNotLostWhenGestureArrivesSlowly()
    {
        var filter = new TapDeduplicator();

        // Tap one: press at 10.0, gesture at 10.35 (ignored).
        Assert.True(filter.TryRegister(Spot, 10f, fromGesture: false));
        Assert.False(filter.TryRegister(Spot, 10.35f, fromGesture: true));

        // Tap two: press at 10.6, gesture at 10.95 (ignored) - exactly one toggle each.
        Assert.True(filter.TryRegister(Spot, 10.6f, fromGesture: false));
        Assert.False(filter.TryRegister(Spot, 10.95f, fromGesture: true));
    }
}
