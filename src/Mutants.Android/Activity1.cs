using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using Mutants.Game;

namespace Mutants.Android;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Icon = "@mipmap/ic_launcher",
    RoundIcon = "@mipmap/ic_launcher_round",
    Theme = "@style/MutantsSplash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Density)]
public class Activity1 : AndroidGameActivity
{
    private MutantsGame? _game;
    private View? _view;

    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);

        Window?.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.KeepScreenOn);

        // API 32+ always has the modern insets controller. Edge-to-edge is
        // enforced from API 35, so only opt in on older releases.
        if (Build.VERSION.SdkInt < BuildVersionCodes.VanillaIceCream)
        {
#pragma warning disable CA1422 // The replacement is only needed on API 35+, where edge-to-edge is enforced.
            Window?.SetDecorFitsSystemWindows(false);
#pragma warning restore CA1422
        }

        if (Window?.InsetsController is { } controller)
        {
            controller.Hide(WindowInsets.Type.SystemBars());
            controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
        }

        _game = new MutantsGame();
        _view = _game.Services.GetService(typeof(View)) as View;
        if (_view is not null)
        {
            SetContentView(_view);
        }

        _game.Run();
    }

    protected override void OnPause()
    {
        _game?.SuppressDraw();
        base.OnPause();
    }

    protected override void OnResume()
    {
        base.OnResume();

        if (Window?.InsetsController is { } controller)
        {
            controller.Hide(WindowInsets.Type.SystemBars());
        }
    }

    protected override void OnDestroy()
    {
        _game?.Dispose();
        _game = null;
        base.OnDestroy();
    }
}
