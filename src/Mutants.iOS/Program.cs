using AVFoundation;
using Foundation;
using Microsoft.Xna.Framework;
using Mutants.Game;
using UIKit;

namespace Mutants.iOS;

[Register("AppDelegate")]
internal sealed class Program : UIApplicationDelegate
{
    private MutantsGame? _game;

    internal static Microsoft.Xna.Framework.Game? Game { get; private set; }

    public static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        ConfigureAudioSession();

        _game = new MutantsGame();
        Game = _game;
        _game.Run();

        // iOS creates the window scene just before or around this call; attach
        // whichever side arrives second so the window always reaches the screen.
        MutantsSceneDelegate.AttachWindow(_game);

        return true;
    }

    /// <summary>
    /// Re-assert the session when the app returns, since a call, Siri or another
    /// app can deactivate it while we are in the background.
    /// </summary>
    public override void OnActivated(UIApplication application)
    {
        var session = AVAudioSession.SharedInstance();
        session.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
        session.SetActive(true);
    }

    /// <summary>
    /// Playback keeps the game audible when the Ring/Silent switch is on, which
    /// is what players expect from a game; mixing means background audio survives.
    /// </summary>
    private static void ConfigureAudioSession()
    {
        var session = AVAudioSession.SharedInstance();
        var categoryError = session.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
        var activeError = session.SetActive(true);

        // Not fatal: without the category the game still plays, it just obeys the
        // Ring/Silent switch. Only surface the failure instead of swallowing it.
        if (categoryError is not null || activeError is not null)
        {
            Console.WriteLine(
                $"[audio] session setup failed: {categoryError?.LocalizedDescription ?? "ok"} / {activeError?.LocalizedDescription ?? "ok"}");
        }
    }
}
